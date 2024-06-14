// <copyright file="ClientService.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

// File may only contain a single type
#pragma warning disable SA1402

using System;

namespace JSSoft.Communication;

[Service(IsServer = false)]
public class ClientService<TServer, TClient>
    : ServiceBase
    where TServer : class
    where TClient : class
{
    private readonly TClient _client;
    private TServer? _server;

    public ClientService(TClient client)
        : base(typeof(TServer), typeof(TClient))
        => _client = client;

    public ClientService()
        : base(typeof(TServer), typeof(TClient))
    {
        var obj = this;
        if (obj is TClient client)
        {
            _client = client;
        }
        else
        {
            throw new InvalidOperationException(
                $"'{GetType()}' must be implemented by '{typeof(TClient)}'.");
        }
    }

    public TServer Server
        => _server ?? throw new InvalidOperationException("Server is not created.");

    protected virtual TClient CreateClient(IPeer peer) => _client;

    protected virtual void DestroyClient(IPeer peer, TClient client)
    {
    }

    protected override sealed object CreateInstance(IPeer peer, object obj)
    {
        _server = (TServer?)obj;
        return CreateClient(peer);
    }

    protected override sealed void DestroyInstance(IPeer peer, object obj)
    {
        DestroyClient(peer, (TClient)obj);
        _server = null;
    }
}

[Service(IsServer = false)]
public class ClientService<TServer>
    : ServiceBase
    where TServer : class
{
    private TServer? _server;

    public ClientService()
        : base(serverType: typeof(TServer), clientType: typeof(void))
    {
    }

    public TServer Server
        => _server ?? throw new InvalidOperationException("Server is not created.");

    protected virtual void OnServiceCreated(IPeer peer, TServer server)
    {
    }

    protected virtual void OnServiceDestroyed(IPeer peer)
    {
    }

    protected override sealed object CreateInstance(IPeer peer, object obj)
    {
        _server = (TServer)obj;
        OnServiceCreated(peer, (TServer)obj);
        return new object();
    }

    protected override sealed void DestroyInstance(IPeer peer, object obj)
    {
        OnServiceDestroyed(peer);
        _server = null;
    }
}
