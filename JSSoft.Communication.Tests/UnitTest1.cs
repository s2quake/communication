namespace JSSoft.Communication.Tests;

public class UnitTest1
{
    public interface ITestService
    {
        [OperationContract]
        Task SendMessage(string message, CancellationToken cancellationToken);
    }

    public interface ITestCallback
    {
        [OperationContract]
        void OnMessageSend(string message);
    }

    sealed class ServerServiceHost : ServerServiceHost<ITestService, ITestCallback>, ITestService
    {
        public Task SendMessage(string message, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    sealed class ClientServiceHost : ClientServiceHost<ITestService, ITestCallback>, ITestCallback
    {
        public void OnMessageSend(string message)
        {
        }
    }

    [Fact]
    public async Task OpenAndClientCloseAsync()
    {
        var server = new ServerContext(new ServerServiceHost());
        var client = new ClientContext(new ClientServiceHost());

        var serverToken = await server.OpenAsync(CancellationToken.None);
        var clientToken = await client.OpenAsync(CancellationToken.None);

        await client.CloseAsync(clientToken, CancellationToken.None);
        await server.CloseAsync(serverToken, CancellationToken.None);
    }

    [Fact]
    public async Task OpenAndServerCloseAsync()
    {
        var server = new ServerContext(new ServerServiceHost());
        var client = new ClientContext(new ClientServiceHost());

        var serverToken = await server.OpenAsync(CancellationToken.None);
        var clientToken = await client.OpenAsync(CancellationToken.None);
        var autoResetEvent = new AutoResetEvent(initialState: false);
        client.Disconnected += (s, e) => autoResetEvent.Set();

        await server.CloseAsync(serverToken, CancellationToken.None);
        if (autoResetEvent.WaitOne(1000) == true)
        {
            await client.CloseAsync(clientToken, CancellationToken.None);
            Assert.Equal(ServiceState.None, client.ServiceState);
        }
        else
        {
            Assert.Fail("Client has not been disconnected.");
        }
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
        var serverCloseTask = server.CloseAsync(serverToken, CancellationToken.None);
        for (var i = 0; i < count; i++)
        {
            closeTasks[i] = clients[i].CloseAsync(openTasks[i].Result, CancellationToken.None);
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
