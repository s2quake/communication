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

using JSSoft.Communication.Tests.Extensions;

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

    protected ClientTestBase(ServerService<TService> serverService)
    {
        ServerService = serverService;
        _serverContext = new(ServerService) { EndPoint = _endPoint };
        _clientContext = new(_clientService) { EndPoint = _endPoint };
    }

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

    protected TService Client => _client!;

    protected ServerService<TService> ServerService { get; }
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

    protected ClientTestBase(TServerSevice serverService)
    {
        ServerService = serverService;
        _serverContext = new(ServerService) { EndPoint = _endPoint };
        _clientContext = new(_clientService) { EndPoint = _endPoint };
    }

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

    protected TService Client => _client!;

    protected TServerSevice ServerService { get; }
}
