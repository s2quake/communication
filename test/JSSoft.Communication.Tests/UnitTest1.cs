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
        using var endPoint = new RandomEndPoint();
        var serverContext = new ServerContext(new TestServer()) { EndPoint = endPoint };
        var clientContext = new ClientContext(new TestClient()) { EndPoint = endPoint };

        var serverToken = await serverContext.OpenAsync(CancellationToken.None);
        var clientToken = await clientContext.OpenAsync(CancellationToken.None);

        await clientContext.CloseAsync(clientToken, CancellationToken.None);
        await serverContext.CloseAsync(serverToken, CancellationToken.None);
    }

    [Fact]
    public async Task OpenAndServerCloseAsync()
    {
        using var endPoint = new RandomEndPoint();
        var serverContext = new ServerContext(new TestServer()) { EndPoint = endPoint };
        var clientContext = new ClientContext(new TestClient()) { EndPoint = endPoint };

        var serverToken = await serverContext.OpenAsync(CancellationToken.None);
        var clientToken = await clientContext.OpenAsync(CancellationToken.None);
        var autoResetEvent = new AutoResetEvent(initialState: false);
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
        var clientContexts = Enumerable.Range(0, count).Select(item => new ClientContext(new TestClient()) { EndPoint = endPoint }).ToArray();
        var serverToken = await serverContext.OpenAsync(CancellationToken.None);
        var openTasks = clientContexts.Select(item => item.OpenAsync(CancellationToken.None)).ToArray();
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
    }
}
