
using JSSoft.Communication.Tests.Extensions;

namespace JSSoft.Communication.Tests;

public class InvokeTest : IAsyncLifetime
{
    private readonly TestServer _serverService = new();
    private readonly ClientService<ITestService> _clientService = new();
    private readonly ServerContext _serverContext;
    private readonly ClientContext _clientContext;
    private ITestService? _client;

    private Guid _clientToken;
    private Guid _serverToken;

    public InvokeTest()
    {
        var endPoint = EndPointUtility.GetEndPoint();
        _serverContext = new(_serverService) { EndPoint = endPoint };
        _clientContext = new(_clientService) { EndPoint = endPoint };
    }

    public interface ITestService
    {
        [ServerMethod]
        void Invoke();

        [ServerMethod(IsOneWay = true)]
        void InvokeOneWay();

        [ServerMethod]
        int InvokeAndReturn();

        [ServerMethod]
        Task InvokeAsync();

        [ServerMethod]
        Task<int> InvokeAndReturnAsync();

        [ServerMethod]
        Task InvokeAsync(CancellationToken cancellationToken);

        [ServerMethod]
        Task<int> InvokeAndReturnAsync(CancellationToken cancellationToken);
    }

    sealed class TestServer : ServerService<ITestService>, ITestService
    {
        public object? Result { get; set; }

        public void Invoke()
        {
            Result = nameof(Invoke);
        }

        public void InvokeOneWay()
        {
            Result = default;
            Thread.Sleep(100);
            Result = nameof(InvokeOneWay);
        }

        public int InvokeAndReturn()
        {
            Result = nameof(InvokeAndReturn);
            return 1;
        }

        public Task InvokeAsync()
        {
            Result = nameof(InvokeAsync);
            return Task.CompletedTask;
        }

        public Task<int> InvokeAndReturnAsync()
        {
            Result = nameof(InvokeAndReturnAsync);
            return Task.Run(() => 2);
        }

        public Task InvokeAsync(CancellationToken cancellationToken)
        {
            Result = nameof(InvokeAsync) + nameof(CancellationToken);
            return Task.CompletedTask;
        }

        public Task<int> InvokeAndReturnAsync(CancellationToken cancellationToken)
        {
            Result = nameof(InvokeAndReturnAsync) + nameof(CancellationToken);
            return Task.Run(() => 3);
        }
    }

    [Fact]
    public void Invoke_Test()
    {
        _client!.Invoke();
        Assert.Equal(nameof(_client.Invoke), _serverService.Result);
    }

    [Fact]
    public void InvokeOneWay_Test()
    {
        _client!.InvokeOneWay();
        Assert.NotEqual(nameof(_client.InvokeOneWay), _serverService.Result);
    }

    [Fact]
    public void InvokeAndReturn_Test()
    {
        var actualValue = _client!.InvokeAndReturn();
        Assert.Equal(nameof(_client.InvokeAndReturn), _serverService.Result);
        Assert.Equal(1, actualValue);
    }

    [Fact]
    public async Task InvokeAsync_Test()
    {
        await _client!.InvokeAsync();
        Assert.Equal(nameof(_client.InvokeAsync), _serverService.Result);
    }

    [Fact]
    public async Task InvokeAndReturnAsync_Test()
    {
        var actualValue = await _client!.InvokeAndReturnAsync();
        Assert.Equal(nameof(_client.InvokeAndReturnAsync), _serverService.Result);
        Assert.Equal(2, actualValue);
    }

    [Fact]
    public async Task InvokeAsyncWithCancellation_Test()
    {
        await _client!.InvokeAsync(CancellationToken.None);
        Assert.Equal(nameof(_client.InvokeAsync) + nameof(CancellationToken), _serverService.Result);
    }

    [Fact]
    public async Task InvokeAndReturnAsyncWithCancellation_Test()
    {
        var actualValue = await _client!.InvokeAndReturnAsync(CancellationToken.None);
        Assert.Equal(nameof(_client.InvokeAndReturnAsync) + nameof(CancellationToken), _serverService.Result);
        Assert.Equal(3, actualValue);
    }

    public async Task InitializeAsync()
    {
        _serverToken = await _serverContext.OpenAsync(CancellationToken.None);
        _clientToken = await _clientContext.OpenAsync(CancellationToken.None);
        _client = _clientService.Server;
    }

    public async Task DisposeAsync()
    {
        await _serverContext.ReleaseAsync(_serverToken);
        await _clientContext.ReleaseAsync(_clientToken);
    }
}
