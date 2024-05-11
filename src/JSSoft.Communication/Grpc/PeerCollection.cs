// MIT License
// 
// Copyright (c) 2024 Jeesu Choi
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JSSoft.Communication.Extensions;
using JSSoft.Communication.Logging;

namespace JSSoft.Communication.Grpc;

sealed class PeerCollection(IInstanceContext instanceContext)
    : ConcurrentDictionary<string, Peer>
{
    private readonly IInstanceContext _instanceContext = instanceContext;

    public void Add(IServiceContext serviceContext, string id)
    {
        var peer = new Peer(id);
        if (TryAdd(id, peer) == true)
        {
            peer.Descriptor = _instanceContext.CreateInstance(peer);
            serviceContext.Debug($"{id}, Connected");
        }
        else
        {
            throw new InvalidOperationException();
        }
    }

    public bool Remove(IServiceContext serviceContext, string id, int closeCode)
    {
        if (TryRemove(id, out var peer) == true)
        {
            peer.Descriptor = null;
            _instanceContext.DestroyInstance(peer);
            peer.Disconect(closeCode);
            serviceContext.Debug($"{id} Disconnected ({closeCode})");
            return true;
        }
        return false;
    }

    public async Task DisconnectAsync(IServiceContext serviceContext, CancellationToken cancellationToken)
    {
        var items = Values.ToArray();
        using var cancellationTokenSource = new CancellationTokenSource(millisecondsDelay: 3000);
        foreach (var item in items)
        {
            item.Disconect(closeCode: 0);
        }
        while (Count > 0 && cancellationTokenSource.IsCancellationRequested != true)
        {
            await Task.Delay(1, cancellationToken);
        }
        Clear();
    }
}
