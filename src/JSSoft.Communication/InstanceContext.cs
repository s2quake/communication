// <copyright file="InstanceContext.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Linq;

namespace JSSoft.Communication;

internal sealed class InstanceContext(ServiceContextBase serviceContext)
    : IInstanceContext, IPeer
{
    private readonly ConcurrentDictionary<IPeer, PeerDescriptor> _descriptorByPeer = new();
    private readonly PeerDescriptor _descriptor = new();
    private readonly ServiceContextBase _serviceContext = serviceContext;

    public string Id => $"{_serviceContext.Id}";

    public void InitializeInstance()
    {
        var query = from item in _serviceContext.Services.Values
                    where ServiceContextBase.IsPerPeer(_serviceContext, item) != true
                    select item;
        foreach (var item in query)
        {
            var (service, callback) = _serviceContext.CreateInstance(item, this);
            _descriptor.AddInstance(item, service, callback);
        }
    }

    public void ReleaseInstance()
    {
        var query = from item in _serviceContext.Services.Values.Reverse()
                    where ServiceContextBase.IsPerPeer(_serviceContext, item) != true
                    select item;
        foreach (var item in query)
        {
            var (service, callback) = _descriptor.RemoveInstance(item);
            _serviceContext.DestroyInstance(item, this, service, callback);
        }
    }

    public PeerDescriptor CreateInstance(IPeer peer)
    {
        var peerDescriptor = new PeerDescriptor();
        foreach (var item in _serviceContext.Services.Values)
        {
            var isPerPeer = ServiceContextBase.IsPerPeer(_serviceContext, item);
            if (isPerPeer == true)
            {
                var (service, callback) = _serviceContext.CreateInstance(item, peer);
                peerDescriptor.AddInstance(item, service, callback);
            }
            else
            {
                var service = _descriptor.ServerInstances[item];
                var callback = _descriptor.ClientInstances[item];
                peerDescriptor.AddInstance(item, service, callback);
            }
        }

        _descriptorByPeer.TryAdd(peer, peerDescriptor);
        return peerDescriptor;
    }

    public void DestroyInstance(IPeer peer)
    {
        if (_descriptorByPeer.TryRemove(peer, out var peerDescriptor) == false)
        {
            return;
        }

        foreach (var item in _serviceContext.Services.Values.Reverse())
        {
            var isPerPeer = ServiceContextBase.IsPerPeer(_serviceContext, item);
            if (isPerPeer == true)
            {
                var (service, callback) = peerDescriptor.RemoveInstance(item);
                _serviceContext.DestroyInstance(item, peer, service, callback);
            }
            else
            {
                peerDescriptor.RemoveInstance(item);
            }
        }

        peerDescriptor.Dispose();
    }

    public object? GetService(Type serviceType)
    {
        var query = from descriptor in _descriptorByPeer.Values
                    from service in descriptor.ServerInstances.Values
                    where serviceType.IsInstanceOfType(service) == true
                    select service;
        return query.SingleOrDefault();
    }
}
