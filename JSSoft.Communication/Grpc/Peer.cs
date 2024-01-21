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
using System.Net.NetworkInformation;
using System.Threading;

namespace JSSoft.Communication.Grpc;

sealed class Peer(string id) : IPeer
{
    private readonly object _lockObject = new();
    private readonly List<CallbackData> _callbackDataList = [];
    private CancellationTokenSource? _cancellationTokenSource;
    private ManualResetEvent? _manualResetEvent;

    public string Id { get; } = id;

    public Guid Token { get; set; } = Guid.NewGuid();

    public DateTime PingTime { get; set; } = DateTime.UtcNow;

    public PeerDescriptor? Descriptor { get; set; }

    public Dictionary<IService, object> Services => Descriptor?.ServerInstances ?? [];

    public int CloseCode { get; private set; } = int.MinValue;

    public CancellationToken Begin(ManualResetEvent manualResetEvent)
    {
        lock (_lockObject)
        {
            _manualResetEvent = manualResetEvent;
            _cancellationTokenSource = new();
            return _cancellationTokenSource.Token;
        }
    }

    public void End()
    {
        lock (_lockObject)
        {
            _cancellationTokenSource = null;
            _manualResetEvent = null;
        }
    }

    public void Disconect(int closeCode)
    {
        lock (_lockObject)
        {
            CloseCode = closeCode;
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = null;
            _manualResetEvent?.Set();
        }
    }

    public PollReply Collect()
    {
        if (_cancellationTokenSource?.IsCancellationRequested == true)
        {
            return new PollReply { Code = CloseCode };
        }

        var items = Flush();
        var reply = new PollReply() { Code = CloseCode };
        foreach (var item in items)
        {
            reply.Items.Add(new PollReplyItem()
            {
                Name = item.Name,
                ServiceName = item.Service.Name,
                Data =
                {
                    item.Data,
                },
            });
        }
        return reply;

        CallbackData[] Flush()
        {
            lock (_lockObject)
            {
                if (_callbackDataList.Count > 0)
                {
                    var items = _callbackDataList.ToArray();
                    _callbackDataList.Clear();
                    return items;
                }
                return [];
            }
        }
    }

    public void Add(CallbackData callbackData)
    {
        lock (_lockObject)
        {
            _callbackDataList.Add(callbackData);
            _manualResetEvent?.Set();
        }
    }
}
