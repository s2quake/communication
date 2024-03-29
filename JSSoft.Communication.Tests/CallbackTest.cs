
using System.Diagnostics;
using JSSoft.Communication.Tests.Extensions;
using Xunit.Abstractions;

namespace JSSoft.Communication.Tests;

public class CallbackTest : IAsyncLifetime
{
    private readonly ITestOutputHelper _logger;
    private readonly TestServer _testServer = new();
    private readonly TestClient _testClient = new();
    private readonly ServerContext _serverContext;
    private readonly ClientContext _clientContext;
    private ITestService? _server;

    private Guid _clientToken;
    private Guid _serverToken;

    public CallbackTest(ITestOutputHelper logger)
    {
        var endPoint = EndPointUtility.GetEndPoint();
        _logger = logger;
        _serverContext = new(_testServer) { EndPoint = endPoint };
        _clientContext = new(_testClient) { EndPoint = endPoint };
    }

    public interface ITestService
    {
        [ServerMethod]
        void Invoke();

        [ServerMethod]
        void Invoke(int value);

        [ServerMethod]
        void Invoke((int value1, string value2) value);
    }

    public interface ITestCallback
    {
        [ClientMethod]
        void OnInvoked();

        [ClientMethod]
        void OnInvoked(int value);

        [ClientMethod]
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
        public event EventHandler<object?>? Invoked;

        void ITestCallback.OnInvoked() => Invoked?.Invoke(this, null);

        void ITestCallback.OnInvoked(int value) => Invoked?.Invoke(this, value);

        void ITestCallback.OnInvoked((int value1, string value2) value)
            => Invoked?.Invoke(this, value);
    }

    [Fact]
    public void Callback1_Test()
    {
        using var manualResetEvent = new ManualResetEvent(initialState: false);
        object? value = null;
        _testClient.Invoked += TestClient_Invoked;
        _server!.Invoke();
        _logger.WriteLine("123123");
        Assert.True(manualResetEvent.WaitOne(3000));
        Assert.Null(value);

        void TestClient_Invoked(object? sender, object? e)
        {
            manualResetEvent.Set();
            _logger.WriteLine("set");
            value = e;
        }
    }

    [Fact]
    public void Callback2_Test()
    {
        using var manualResetEvent = new ManualResetEvent(initialState: false);
        object? value = new();
        _testClient.Invoked += TestClient_Invoked;
        _server!.Invoke(123);
        Assert.True(manualResetEvent.WaitOne(3000));
        Assert.Equal(123, value);

        void TestClient_Invoked(object? sender, object? e)
        {
            manualResetEvent.Set();
            value = e;
        }
    }

    [Fact]
    public void Callback3_Test()
    {
        using var manualResetEvent = new ManualResetEvent(initialState: false);
        object? value = new();
        _testClient.Invoked += TestClient_Invoked;
        _server!.Invoke((123, "123"));
        Assert.True(manualResetEvent.WaitOne(3000));
        Assert.Equal((123, "123"), value);

        void TestClient_Invoked(object? sender, object? e)
        {
            manualResetEvent.Set();
            value = e;
        }
    }

    public async Task InitializeAsync()
    {
        _serverToken = await _serverContext.OpenAsync(CancellationToken.None);
        _clientToken = await _clientContext.OpenAsync(CancellationToken.None);
        _server = _testServer;
    }

    public async Task DisposeAsync()
    {
        await _serverContext.ReleaseAsync(_serverToken);
        await _clientContext.ReleaseAsync(_clientToken);
    }
}
