
using JSSoft.Communication.Tests.Extensions;

namespace JSSoft.Communication.Tests;

public class InvokeTest : IAsyncLifetime
{
    private readonly TestServer _server = new();
    private readonly ClientService<ITestService> _clientService = new();
    private readonly ServerContext _serverContext;
    private readonly ClientContext _clientContext;
    private ITestService? _client;

    private Guid _clientToken;
    private Guid _serverToken;

    public InvokeTest()
    {
        _serverContext = new(_server);
        _clientContext = new(_clientService);
    }

    public interface ITestService
    {
        [ServerMethod]
        void Invoke();

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

    sealed class TestServer : ServerService<ITestService, ITestService>, ITestService
    {
        public string Result { get; set; } = string.Empty;

        public void Invoke()
        {
            Result = "a";
        }

        public int InvokeAndReturn()
        {
            return 1;
        }

        public Task InvokeAsync()
        {
            Result = "b";
            return Task.CompletedTask;
        }

        public Task<int> InvokeAndReturnAsync()
        {
            return Task.Run(() => 2);
        }

        public Task InvokeAsync(CancellationToken cancellationToken)
        {
            Result = "c";
            return Task.CompletedTask;
        }

        public Task<int> InvokeAndReturnAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() => 3);
        }
    }

    [Fact]
    public void Invoke_Test()
    {
        _client!.Invoke();
        Assert.Equal("a", _server.Result);
    }

    [Fact]
    public void InvokeAndReturn_Test()
    {
        var actualValue = _client!.InvokeAndReturn();
        Assert.Equal(1, actualValue);
    }

    [Fact]
    public async Task InvokeAsync_Test()
    {
        await _client!.InvokeAsync();
        Assert.Equal("b", _server.Result);
    }

    [Fact]
    public async Task InvokeAndReturnAsync_Test()
    {
        var actualValue = await _client!.InvokeAndReturnAsync();
        Assert.Equal(2, actualValue);
    }

    [Fact]
    public async Task InvokeAsync2_Test()
    {
        await _client!.InvokeAsync(CancellationToken.None);
        Assert.Equal("c", _server.Result);
    }

    [Fact]
    public async Task InvokeAndReturnAsync2_Test()
    {
        var actualValue = await _client!.InvokeAndReturnAsync(CancellationToken.None);
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
