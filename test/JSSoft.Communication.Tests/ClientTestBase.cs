// <copyright file="ClientTestBase.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

// File may only contain a single type
#pragma warning disable SA1402

using JSSoft.Communication.Tests.Extensions;
using Xunit.Abstractions;

namespace JSSoft.Communication.Tests;

public abstract class ClientTestBase<TService> : IAsyncLifetime
    where TService : class
{
    private readonly ClientService<TService> _clientService = new();
    private readonly ServerContext _serverContext;
    private readonly ClientContext _clientContext;
    private readonly RandomEndPoint _endPoint = new();
    private TService? _client;

    private Guid _clientToken;
    private Guid _serverToken;

    protected ClientTestBase(ITestOutputHelper logger, ServerService<TService> serverService)
    {
        Logger = logger;
        ServerService = serverService;
        _serverContext = new(ServerService) { EndPoint = _endPoint };
        _clientContext = new(_clientService) { EndPoint = _endPoint };
        Logger.WriteLine($"EndPoint: {_endPoint}");
    }

    protected ITestOutputHelper Logger { get; }

    protected TService Client => _client!;

    protected ServerService<TService> ServerService { get; }

    public async Task InitializeAsync()
    {
        _serverToken = await _serverContext.OpenAsync(cancellationToken: default);
        Logger.WriteLine($"Server is opened: {_serverToken}");
        _clientToken = await _clientContext.OpenAsync(cancellationToken: default);
        Logger.WriteLine($"Client is opened: {_clientToken}");
        _client = _clientService.Server;
    }

    public async Task DisposeAsync()
    {
        await _serverContext.ReleaseAsync(_serverToken);
        Logger.WriteLine($"Server is released: {_serverToken}");
        await _clientContext.ReleaseAsync(_clientToken);
        Logger.WriteLine($"Client is released: {_clientToken}");
        _endPoint.Dispose();
    }
}

public abstract class ClientTestBase<TService, TServerSevice> : IAsyncLifetime
    where TService : class
    where TServerSevice : ServerService<TService>
{
    private readonly ClientService<TService> _clientService = new();
    private readonly ServerContext _serverContext;
    private readonly ClientContext _clientContext;
    private readonly RandomEndPoint _endPoint = new();
    private TService? _client;

    private Guid _clientToken;
    private Guid _serverToken;

    protected ClientTestBase(ITestOutputHelper logger, TServerSevice serverService)
    {
        Logger = logger;
        ServerService = serverService;
        _serverContext = new(ServerService) { EndPoint = _endPoint };
        _clientContext = new(_clientService) { EndPoint = _endPoint };
    }

    protected ITestOutputHelper Logger { get; }

    protected TService Client => _client!;

    protected TServerSevice ServerService { get; }

    public async Task InitializeAsync()
    {
        _serverToken = await _serverContext.OpenAsync(cancellationToken: default);
        _clientToken = await _clientContext.OpenAsync(cancellationToken: default);
        _client = _clientService.Server;
    }

    public async Task DisposeAsync()
    {
        await _serverContext.ReleaseAsync(_serverToken);
        await _clientContext.ReleaseAsync(_clientToken);
        _endPoint.Dispose();
    }
}
