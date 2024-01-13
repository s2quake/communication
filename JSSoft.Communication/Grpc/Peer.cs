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
    private readonly IService[] _services;
    private bool _isDisposed;

    public Peer(string id, IService[] services)
    {
        Id = id;
        _services = services;
        Ping(DateTime.UtcNow);
        foreach (var item in services)
        {
            PollReplyItems.Add(item, []);
        }
    }

    public string Id { get; }

    public Guid Token { get; set; } = Guid.NewGuid();

    public DateTime PingTime { get; set; }

    public PeerDescriptor? Descriptor { get; set; }

    public Dictionary<IService, object> Services => Descriptor?.ServerInstances ?? [];

    public Dictionary<IService, PollReplyItemCollection> PollReplyItems { get; } = [];

    public void Dispose()
    {
        if (_isDisposed == true)
            throw new ObjectDisposedException($"{this}");

        lock (this)
        {
            foreach (var item in _services)
            {
                PollReplyItems.Remove(item);
            }
            _isDisposed = true;
        }
        GC.SuppressFinalize(this);
    }

    public void Ping(DateTime dateTime)
    {
        PingTime = dateTime;
    }

    public PollReply Collect()
    {
        var reply = new PollReply() { Code = int.MinValue };
        lock (this)
        {
            if (_isDisposed != true)
            {
                var services = _services;
                foreach (var item in services)
                {
                    var callbacks = PollReplyItems[item];
                    var items = callbacks.Flush();
                    reply.Items.AddRange(items);
                }
            }
        }
        return reply;
    }
}
