// <copyright file="Peer.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;

namespace JSSoft.Communication.Grpc;

internal sealed class Peer(string id) : IPeer
{
    private readonly object _lockObject = new();
    private readonly List<CallbackData> _callbackDataList = [];
    private CancellationTokenSource? _cancellationTokenSource;
    private ManualResetEvent? _manualResetEvent;

    public string Id { get; } = id;

    public DateTime PingTime { get; set; } = DateTime.UtcNow;

    public PeerDescriptor? Descriptor { get; set; }

    public Dictionary<IService, object> Services => Descriptor?.ServerInstances ?? [];

    public int CloseCode { get; private set; } = int.MinValue;

    public bool CanCollect => _callbackDataList.Count > 0;

    public CancellationToken BeginPolling(ManualResetEvent manualResetEvent)
    {
        lock (_lockObject)
        {
            _manualResetEvent = manualResetEvent;
            _cancellationTokenSource = new();
            return _cancellationTokenSource.Token;
        }
    }

    public void EndPolling()
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
            _cancellationTokenSource?.Dispose();
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
                Data = { item.Data },
            });
        }

        return reply;
    }

    public void AddCallback(CallbackData callbackData)
    {
        lock (_lockObject)
        {
            _callbackDataList.Add(callbackData);
            _manualResetEvent?.Set();
        }
    }

    private CallbackData[] Flush()
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
