// <copyright file="PeerDescriptor.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;

namespace JSSoft.Communication;

public sealed class PeerDescriptor : IDisposable
{
    private bool _isDisposed;

    public Dictionary<IService, object> ServerInstances { get; } = [];

    public Dictionary<IService, object> ClientInstances { get; } = [];

    public void Dispose()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var items = ClientInstances.Values.OfType<IDisposable>().ToArray();
        foreach (var item in items)
        {
            item.Dispose();
        }

        _isDisposed = true;
    }

    public void AddInstance(IService service, object serverInstance, object clientInstance)
    {
        if (_isDisposed == true)
        {
            throw new ObjectDisposedException($"{this}");
        }

        ServerInstances.Add(service, serverInstance);
        ClientInstances.Add(service, clientInstance);
    }

    public (object ServerInstance, object ClientInstance) RemoveInstance(IService service)
    {
        if (_isDisposed == true)
        {
            throw new ObjectDisposedException($"{this}");
        }

        var value = (ServerInstances[service], ClientInstances[service]);
        ServerInstances.Remove(service);
        ClientInstances.Remove(service);
        return value;
    }
}
