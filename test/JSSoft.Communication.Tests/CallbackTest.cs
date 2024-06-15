// <copyright file="CallbackTest.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using JSSoft.Communication.Tests.Extensions;
using Xunit.Abstractions;

namespace JSSoft.Communication.Tests;

public class CallbackTest : IAsyncLifetime
{
    private const int ClientCount = 2;
    private readonly ITestOutputHelper _logger;
    private readonly TestServer _testServer = new();
    private readonly ServerContext _serverContext;
    private readonly TestClient[] _testClients = new TestClient[ClientCount];
    private readonly ClientContext[] _clientContexts = new ClientContext[ClientCount];
    private readonly Guid[] _clientTokens = new Guid[ClientCount];
    private readonly RandomEndPoint _endPoint = new();
    private TestServer? _server;

    private Guid _serverToken;

    public CallbackTest(ITestOutputHelper logger)
    {
        _logger = logger;
        _serverContext = new(_testServer) { EndPoint = _endPoint };
        for (var i = 0; i < ClientCount; i++)
        {
            _testClients[i] = new() { Index = i };
            _clientContexts[i] = new(_testClients[i]) { EndPoint = _endPoint };
        }

        logger.WriteLine($"{_endPoint}");
    }

    public interface ITestService
    {
        void Invoke();

        void Invoke(int value);

        void Invoke((int Value1, string Value2) value);
    }

    public interface ITestCallback
    {
        void OnInvoked();

        void OnInvoked(int value);

        void OnInvoked((int Value1, string Value2) value);
    }

    [Fact]
    public async Task Callback1_TestAsync()
    {
        var raisedCount = await EventTestUtility.RaisesManyAsync<TestClient, ValueEventArgs>(
            items: _testClients,
            attach: (item, handler) => item.Invoked += handler,
            detach: (item, handler) => item.Invoked -= handler,
            testCode: () =>
            {
                _server!.Invoke();
            });

        Assert.Equal(_testClients.Length, raisedCount);
        Assert.All(_testClients, item => Assert.Null(item.Value));
    }

    [Fact]
    public async Task Callback2_TestAsync()
    {
        var value = 123;
        var raisedCount = await EventTestUtility.RaisesManyAsync<TestClient, ValueEventArgs>(
            items: _testClients,
            attach: (item, handler) => item.Invoked += handler,
            detach: (item, handler) => item.Invoked -= handler,
            testCode: () =>
            {
                _server!.Invoke(value);
            });
        Assert.Equal(_testClients.Length, raisedCount);
        Assert.All(_testClients, item => Assert.Equal(value, item.Value));
    }

    [Fact]
    public async Task Callback3_TestAsync()
    {
        var value = (123, "123");
        var raisedCount = await EventTestUtility.RaisesManyAsync<TestClient, ValueEventArgs>(
            items: _testClients,
            attach: (item, handler) => item.Invoked += handler,
            detach: (item, handler) => item.Invoked -= handler,
            testCode: () =>
            {
                _server!.Invoke(value);
            });
        Assert.Equal(_testClients.Length, raisedCount);
        Assert.All(_testClients, item => Assert.Equal(value, item.Value));
    }

    public async Task InitializeAsync()
    {
        _serverToken = await _serverContext.OpenAsync(CancellationToken.None);
        _logger.WriteLine($"Server is opened: {_serverToken}");
        for (var i = 0; i < ClientCount; i++)
        {
            _clientTokens[i] = await _clientContexts[i].OpenAsync(CancellationToken.None);
            _logger.WriteLine($"Client #{i} is opened: {_clientTokens[i]}");
        }

        _server = _testServer;
    }

    public async Task DisposeAsync()
    {
        await _serverContext.ReleaseAsync(_serverToken);
        _logger.WriteLine($"Server is released: {_serverToken}");
        for (var i = 0; i < ClientCount; i++)
        {
            await _clientContexts[i].ReleaseAsync(_clientTokens[i]);
            _logger.WriteLine($"Client #{i} is released: {_clientTokens[i]}");
        }

        _endPoint.Dispose();
    }

    public class ValueEventArgs(object? value) : EventArgs
    {
        public object? Value { get; } = value;
    }

    private sealed class TestServer : ServerService<ITestService, ITestCallback>, ITestService
    {
        public void Invoke() => Client.OnInvoked();

        public void Invoke(int value) => Client.OnInvoked(value);

        public void Invoke((int Value1, string Value2) value) => Client.OnInvoked(value);
    }

    private sealed class TestClient : ClientService<ITestService, ITestCallback>, ITestCallback
    {
        public event EventHandler<ValueEventArgs>? Invoked;

        public AutoResetEvent AutoResetEvent { get; } = new(initialState: false);

        public int Index { get; set; } = -1;

        public object? Value { get; private set; } = DBNull.Value;

        void ITestCallback.OnInvoked()
        {
            Value = null;
            Invoked?.Invoke(this, new(null));
            AutoResetEvent.Set();
        }

        void ITestCallback.OnInvoked(int value)
        {
            Value = value;
            Invoked?.Invoke(this, new(value));
            AutoResetEvent.Set();
        }

        void ITestCallback.OnInvoked((int Value1, string Value2) value)
        {
            Value = value;
            Invoked?.Invoke(this, new(value));
            AutoResetEvent.Set();
        }
    }
}
