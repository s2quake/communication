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
using Xunit.Abstractions;

namespace JSSoft.Communication.Tests;

public class CallbackTest : IAsyncLifetime
{
    private const int Timeout = 30000;
    private readonly ITestOutputHelper _logger;
    private readonly TestServer _testServer = new();
    private readonly TestClient _testClient = new();
    private readonly ServerContext _serverContext;
    private readonly ClientContext _clientContext;
    private readonly RandomEndPoint _endPoint = new();
    private ITestService? _server;

    private Guid _clientToken;
    private Guid _serverToken;

    public CallbackTest(ITestOutputHelper logger)
    {
        _logger = logger;
        _serverContext = new(_testServer) { EndPoint = _endPoint };
        _clientContext = new(_testClient) { EndPoint = _endPoint };
        logger.WriteLine($"{_endPoint}");
    }

    public class ValueEventArgs(object? value) : EventArgs
    {
        public object? Value { get; } = value;
    }

    public interface ITestService
    {
        void Invoke();

        void Invoke(int value);

        void Invoke((int value1, string value2) value);
    }

    public interface ITestCallback
    {
        void OnInvoked();

        void OnInvoked(int value);

        void OnInvoked((int value1, string value2) value);
    }

    sealed class TestServer : ServerService<ITestService, ITestCallback>, ITestService
    {
        public void Invoke() => Client.OnInvoked();

        public void Invoke(int value) => Client.OnInvoked(value);

        public void Invoke((int value1, string value2) value) => Client.OnInvoked(value);
    }

    sealed class TestClient : ClientService<ITestService, ITestCallback>, ITestCallback
    {
        public AutoResetEvent AutoResetEvent { get; } = new(initialState: false);

        public event EventHandler<ValueEventArgs>? Invoked;

        void ITestCallback.OnInvoked()
        {
            Invoked?.Invoke(this, new(null));
            AutoResetEvent.Set();
        }

        void ITestCallback.OnInvoked(int value)
        {
            Invoked?.Invoke(this, new(value));
            AutoResetEvent.Set();
        }

        void ITestCallback.OnInvoked((int value1, string value2) value)
        {
            Invoked?.Invoke(this, new(value));
            AutoResetEvent.Set();
        }
    }

    [Fact]
    public void Callback1_Test()
    {
        var raised = Assert.Raises<ValueEventArgs>(
            handler => _testClient.Invoked += handler,
            handler => _testClient.Invoked -= handler,
            () =>
            {
                _server!.Invoke();
                _testClient.AutoResetEvent.WaitOne(Timeout);
            });
        Assert.Null(raised.Arguments.Value);
    }

    [Fact]
    public void Callback2_Test()
    {
        var value = 123;
        var raised = Assert.Raises<ValueEventArgs>(
            handler => _testClient.Invoked += handler,
            handler => _testClient.Invoked -= handler,
            () =>
            {
                _server!.Invoke(value);
                _testClient.AutoResetEvent.WaitOne(Timeout);
            });
        Assert.Equal(value, raised.Arguments.Value);
    }

    [Fact]
    public void Callback3_Test()
    {
        var value = (123, "123");
        var raised = Assert.Raises<ValueEventArgs>(
            handler => _testClient.Invoked += handler,
            handler => _testClient.Invoked -= handler,
            () =>
            {
                _server!.Invoke(value);
                _testClient.AutoResetEvent.WaitOne(Timeout);
            });
        Assert.Equal(value, raised.Arguments.Value);
    }

    public async Task InitializeAsync()
    {
        _logger.WriteLine($"InitializeAsync 1");
        _serverToken = await _serverContext.OpenAsync(CancellationToken.None);
        _clientToken = await _clientContext.OpenAsync(CancellationToken.None);
        _server = _testServer;
        _logger.WriteLine($"InitializeAsync 2");
    }

    public async Task DisposeAsync()
    {
        _logger.WriteLine($"DisposeAsync 1");
        await _serverContext.ReleaseAsync(_serverToken);
        await _clientContext.ReleaseAsync(_clientToken);
        _endPoint.Dispose();
        _logger.WriteLine($"DisposeAsync 2");
    }
}
