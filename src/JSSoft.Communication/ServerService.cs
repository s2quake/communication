// <copyright file="ServerService.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

// File may only contain a single type
#pragma warning disable SA1402

using System;

namespace JSSoft.Communication;

[Service(IsServer = true)]
public class ServerService<TServer, TClient>
    : ServiceBase
    where TServer : class
    where TClient : class
{
    private readonly TServer _server;
    private TClient? _client;

    public ServerService(TServer server)
        : base(typeof(TServer), typeof(TClient))
        => _server = server;

    public ServerService()
        : base(typeof(TServer), typeof(TClient))
    {
        var obj = this;
        if (obj is TServer server)
        {
            _server = server;
        }
        else
        {
            throw new InvalidOperationException(
                $"'{GetType()}' must be implemented by '{typeof(TServer)}'.");
        }
    }

    public TClient Client
        => _client ?? throw new InvalidOperationException("Client is not created.");

    protected virtual TServer CreateServer(IPeer peer) => _server;

    protected virtual void DestroyServer(IPeer peer, TServer server)
    {
    }

    protected override sealed object CreateInstance(IPeer peer, object obj)
    {
        _client = (TClient)obj;
        return CreateServer(peer);
    }

    protected override sealed void DestroyInstance(IPeer peer, object obj)
    {
        DestroyServer(peer, (TServer)obj);
        _client = null;
    }
}

[Service(IsServer = true)]
public class ServerService<TServer> : ServiceBase
    where TServer : class
{
    private readonly TServer? _server;

    public ServerService(TServer server)
        : base(serverType: typeof(TServer), clientType: typeof(void))
        => _server = server;

    public ServerService()
        : base(serverType: typeof(TServer), clientType: typeof(void))
    {
        var obj = this;
        if (obj is TServer server)
        {
            _server = server;
        }
        else
        {
            throw new InvalidOperationException(
                $"'{GetType()}' must be implemented by '{typeof(TServer)}'.");
        }
    }

    protected virtual TServer CreateServer(IPeer peer) => _server!;

    protected virtual void DestroyServer(IPeer peer, TServer server)
    {
    }

    protected override sealed object CreateInstance(IPeer peer, object obj)
        => CreateServer(peer);

    protected override sealed void DestroyInstance(IPeer peer, object obj)
        => DestroyServer(peer, (TServer)obj);
}
