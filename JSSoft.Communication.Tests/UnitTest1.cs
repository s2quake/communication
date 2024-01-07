

namespace JSSoft.Communication.Tests;

public class UnitTest1
{
    public interface ITestService
    {
    }

    sealed class ServerServiceHost : ServerServiceHost<ITestService>, ITestService
    {
    }

    sealed class ClientServiceHost : ClientServiceHost<ITestService>
    {
    }

    [Fact]
    public async Task OpenAndClientCloseAsync()
    {
        var server = new ServerContext(new ServerServiceHost());
        var client = new ClientContext(new ClientServiceHost());

        var serverToken = await server.OpenAsync(CancellationToken.None);
        var clientToken = await client.OpenAsync(CancellationToken.None);

        await client.CloseAsync(clientToken, 0, CancellationToken.None);
        await server.CloseAsync(serverToken, 0, CancellationToken.None);
    }

    [Fact]
    public async Task OpenAndServerCloseAsync()
    {
        var server = new ServerContext(new ServerServiceHost());
        var client = new ClientContext(new ClientServiceHost());

        var serverToken = await server.OpenAsync(CancellationToken.None);
        var clientToken = await client.OpenAsync(CancellationToken.None);
        var autoResetEvent = new AutoResetEvent(initialState: false);
        client.Closed += (s, e) => autoResetEvent.Set();
        client.Faulted += (s, e) => throw new NotImplementedException();

        await server.CloseAsync(serverToken, 0, CancellationToken.None);

        var result = autoResetEvent.WaitOne(millisecondsTimeout: 1000);
        Assert.True(result);
        Assert.Equal(ServiceState.None, client.ServiceState);
    }
}
