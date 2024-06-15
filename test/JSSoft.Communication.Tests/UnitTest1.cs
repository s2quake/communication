// <copyright file="UnitTest1.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

namespace JSSoft.Communication.Tests;

public class UnitTest1
{
    public interface ITestServer
    {
        Task SendMessageAsync(string message, CancellationToken cancellationToken);
    }

    public interface ITestClient
    {
        void OnMessageSend(string message);
    }

    [Fact]
    public async Task OpenAndClientCloseAsync()
    {
        using var endPoint = new RandomEndPoint();
        var serverContext = new ServerContext(new TestServer()) { EndPoint = endPoint };
        var clientContext = new ClientContext(new TestClient()) { EndPoint = endPoint };

        var serverToken = await serverContext.OpenAsync(CancellationToken.None);
        var clientToken = await clientContext.OpenAsync(CancellationToken.None);

        await clientContext.CloseAsync(clientToken, CancellationToken.None);
        await serverContext.CloseAsync(serverToken, CancellationToken.None);

        Assert.Equal(ServiceState.None, clientContext.ServiceState);
        Assert.Equal(ServiceState.None, serverContext.ServiceState);
    }

    [Fact]
    public async Task OpenAndServerCloseAsync()
    {
        using var endPoint = new RandomEndPoint();
        var serverContext = new ServerContext(new TestServer()) { EndPoint = endPoint };
        var clientContext = new ClientContext(new TestClient()) { EndPoint = endPoint };

        var autoResetEvent = new AutoResetEvent(initialState: false);
        var serverToken = await serverContext.OpenAsync(CancellationToken.None);
        await clientContext.OpenAsync(CancellationToken.None);
        clientContext.Disconnected += (s, e) => autoResetEvent.Set();

        await serverContext.CloseAsync(serverToken, CancellationToken.None);
        if (autoResetEvent.WaitOne(1000) == true)
        {
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
        using var endPoint = new RandomEndPoint();
        var serverContext = new ServerContext(new TestServer()) { EndPoint = endPoint };
        var clientContexts = Enumerable.Range(0, count)
                                       .Select(CreateClientContext)
                                       .ToArray();
        var serverToken = await serverContext.OpenAsync(CancellationToken.None);
        var openTasks = clientContexts.Select(item => item.OpenAsync(CancellationToken.None))
                                      .ToArray();
        var tokens = await Task.WhenAll(openTasks);
        var closeTasks = new Task[count];
        for (var i = 0; i < count; i++)
        {
            closeTasks[i] = clientContexts[i].CloseAsync(tokens[i], CancellationToken.None);
        }

        await Task.WhenAll(closeTasks);
        await serverContext.CloseAsync(serverToken, CancellationToken.None);
        Assert.Equal(ServiceState.None, serverContext.ServiceState);
        for (var i = 0; i < count; i++)
        {
            Assert.Equal(ServiceState.None, clientContexts[i].ServiceState);
        }

        ClientContext CreateClientContext(int index)
        {
            return new ClientContext(new TestClient())
            {
                EndPoint = endPoint,
            };
        }
    }

    [Fact]
    public async Task OpenAndInvokeAndClientCloseAsync()
    {
        using var endPoint = new RandomEndPoint();
        var server = new TestServer();
        var client = new TestClient();
        var serverContext = new ServerContext(server) { EndPoint = endPoint };
        var clientContext = new ClientContext(client) { EndPoint = endPoint };

        var serverToken = await serverContext.OpenAsync(CancellationToken.None);
        var clientToken = await clientContext.OpenAsync(CancellationToken.None);

        await client.Server.SendMessageAsync("123", cancellationToken: default);

        await clientContext.CloseAsync(clientToken, CancellationToken.None);
        await serverContext.CloseAsync(serverToken, CancellationToken.None);

        Assert.Equal(ServiceState.None, clientContext.ServiceState);
        Assert.Equal(ServiceState.None, serverContext.ServiceState);
    }

    private sealed class TestServer : ServerService<ITestServer, ITestClient>, ITestServer
    {
        public async Task SendMessageAsync(string message, CancellationToken cancellationToken)
        {
            await Task.Delay(1000, cancellationToken);
        }
    }

    private sealed class TestClient : ClientService<ITestServer, ITestClient>, ITestClient
    {
        public void OnMessageSend(string message)
        {
        }
    }
}
