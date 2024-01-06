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
using System.Collections.Generic;

namespace JSSoft.Communication.Grpc;

sealed class Peer : IPeer, IDisposable
{
    private bool _isDisposed;

    public Peer(Guid id, IServiceHost[] serviceHosts)
    {
        ID = id;
        ServiceHosts = serviceHosts;
        Ping(DateTime.UtcNow);
        foreach (var item in serviceHosts)
        {
            PollReplyItems.Add(item, []);
        }
    }

    public void Dispose()
    {
        if (_isDisposed == true)
            throw new ObjectDisposedException($"{this}");

        foreach (var item in ServiceHosts)
        {
            PollReplyItems.Remove(item);
        }
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }

    public void Ping(DateTime dateTime)
    {
        PingTime = dateTime;
    }

    public Guid ID { get; }

    public IServiceHost[] ServiceHosts { get; }

    public Guid Token { get; set; } = Guid.NewGuid();

    public DateTime PingTime { get; set; }

    public PeerDescriptor? Descriptor { get; set; }

    public Dictionary<IServiceHost, object> Services => Descriptor?.Services ?? [];

    public Dictionary<IServiceHost, PollReplyItemCollection> PollReplyItems { get; } = [];
}
