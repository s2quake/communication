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

        await server.CloseAsync(serverToken, 0, CancellationToken.None);
        await Task.Delay(1000);
        Assert.Equal(ServiceState.Disconnected, client.ServiceState);
        await client.CloseAsync(clientToken, 0, CancellationToken.None);

        Assert.Equal(ServiceState.None, client.ServiceState);
    }

    [Fact]
    public async Task MultipleOpenAndClientCloseAsync()
    {
        var count = 20;
        var server = new ServerContext(new ServerServiceHost());
        var clients = Enumerable.Range(0, count).Select(item => new ClientContext(new ClientServiceHost())).ToArray();
        var serverToken = await server.OpenAsync(CancellationToken.None);
        var openTasks = clients.Select(item => item.OpenAsync(CancellationToken.None)).ToArray();
        await Task.WhenAll(openTasks);
        var closeTasks = new Task[count];
        var serverCloseTask = server.CloseAsync(serverToken, 0, CancellationToken.None);
        for (var i = 0; i < count; i++)
        {
            closeTasks[i] = clients[i].CloseAsync(openTasks[i].Result, 0, CancellationToken.None);
        }
        await Task.WhenAll(closeTasks);
        await serverCloseTask;
        Assert.Equal(ServiceState.None, server.ServiceState);
        for (var i = 0; i < count; i++)
        {
            Assert.Equal(ServiceState.None, clients[i].ServiceState);
        }
    }
}
