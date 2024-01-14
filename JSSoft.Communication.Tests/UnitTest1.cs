namespace JSSoft.Communication.Tests;

public class UnitTest1
{
    public interface ITestServer
    {
        [ServerMethod]
        Task SendMessageAsync(string message, CancellationToken cancellationToken);
    }

    public interface ITestClient
    {
        [ClientMethod]
        void OnMessageSend(string message);
    }

    sealed class TestServer : ServerService<ITestServer, ITestClient>, ITestServer
    {
        public async Task SendMessageAsync(string message, CancellationToken cancellationToken)
        {
            await Task.Delay(1000, cancellationToken);
        }
    }

    sealed class TestClient : ClientService<ITestServer, ITestClient>, ITestClient
    {
        public void OnMessageSend(string message)
        {
        }
    }

    [Fact]
    public async Task OpenAndClientCloseAsync()
    {
        var serverContext = new ServerContext(new TestServer());
        var clientContext = new ClientContext(new TestClient());

        var serverToken = await serverContext.OpenAsync(CancellationToken.None);
        var clientToken = await clientContext.OpenAsync(CancellationToken.None);

        await clientContext.CloseAsync(clientToken, CancellationToken.None);
        await serverContext.CloseAsync(serverToken, CancellationToken.None);
    }

    [Fact]
    public async Task OpenAndServerCloseAsync()
    {
        var serverContext = new ServerContext(new TestServer());
        var clientContext = new ClientContext(new TestClient());

        var serverToken = await serverContext.OpenAsync(CancellationToken.None);
        var clientToken = await clientContext.OpenAsync(CancellationToken.None);
        var autoResetEvent = new AutoResetEvent(initialState: false);
        clientContext.Disconnected += (s, e) => autoResetEvent.Set();

        await serverContext.CloseAsync(serverToken, CancellationToken.None);
        if (autoResetEvent.WaitOne(1000) == true)
        {
            await clientContext.CloseAsync(clientToken, CancellationToken.None);
            Assert.Equal(ServiceState.None, clientContext.ServiceState);
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
        var serverContext = new ServerContext(new TestServer());
        var clientContexts = Enumerable.Range(0, count).Select(item => new ClientContext(new TestClient())).ToArray();
        var serverToken = await serverContext.OpenAsync(CancellationToken.None);
        var openTasks = clientContexts.Select(item => item.OpenAsync(CancellationToken.None)).ToArray();
        await Task.WhenAll(openTasks);
        var closeTasks = new Task[count];
        var serverCloseTask = serverContext.CloseAsync(serverToken, CancellationToken.None);
        for (var i = 0; i < count; i++)
        {
            closeTasks[i] = clientContexts[i].CloseAsync(openTasks[i].Result, CancellationToken.None);
        }
        await Task.WhenAll(closeTasks);
        await serverCloseTask;
        Assert.Equal(ServiceState.None, serverContext.ServiceState);
        for (var i = 0; i < count; i++)
        {
            Assert.Equal(ServiceState.None, clientContexts[i].ServiceState);
        }
    }

    [Fact]
    public async Task OpenAndInvokeAndClientCloseAsync()
    {
        var server = new TestServer();
        var client = new TestClient();
        var serverContext = new ServerContext(server);
        var clientContext = new ClientContext(client);

        var serverToken = await serverContext.OpenAsync(CancellationToken.None);
        var clientToken = await clientContext.OpenAsync(CancellationToken.None);

        var cancellationTokenSource = new CancellationTokenSource(500);
        await client.Server.SendMessageAsync("123", cancellationTokenSource.Token);

        await clientContext.CloseAsync(clientToken, CancellationToken.None);
        await serverContext.CloseAsync(serverToken, CancellationToken.None);
    }
}
