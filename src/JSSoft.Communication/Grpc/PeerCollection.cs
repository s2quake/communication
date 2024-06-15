// <copyright file="PeerCollection.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JSSoft.Communication.Extensions;
using JSSoft.Communication.Logging;

namespace JSSoft.Communication.Grpc;

internal sealed class PeerCollection(IInstanceContext instanceContext)
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
            throw new InvalidOperationException($"{id} is already exists");
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

    public async Task DisconnectAsync(
        IServiceContext serviceContext, CancellationToken cancellationToken)
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
