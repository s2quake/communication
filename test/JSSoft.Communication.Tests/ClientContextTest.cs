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

using JSSoft.Communication.Extensions;

namespace JSSoft.Communication.Tests;

public sealed class ClientContextTest : IAsyncLifetime
{
    private readonly ServerContext _serverContext;
    private readonly RandomEndPoint _endPoint = new();
    private Guid _token;

    public interface ITestService1 : IService
    {
    }

    public interface ITestService2 : IService
    {
    }

    sealed class TestService1 : ClientService<ITestService1>
    {
    }

    sealed class TestService2 : ClientService<ITestService2>
    {
    }

    public ClientContextTest()
    {
        _serverContext = new() { EndPoint = _endPoint };
    }

    [Fact]
    public void Constructor_Test()
    {
        var clientContext0 = new ClientContext();
        Assert.Empty(clientContext0.Services);
        var clientContext1 = new ClientContext(services:
        [
            new TestService2(),
        ]);
        Assert.Single(clientContext1.Services);
        var clientContext2 = new ClientContext(services:
        [
            new TestService1(),
            new TestService2(),
        ]);
        Assert.Equal(2, clientContext2.Services.Count);
    }

    [Fact]
    public void Constructor_WithSameTypeServices_FailTest()
    {
        var services = new IService[] { new TestService1(), new TestService1() };
        Assert.Throws<ArgumentException>(() => new ClientContext(services));
    }

    [Fact]
    public void EndPoint_Test()
    {
        var endPoint = _serverContext.EndPoint;
        var clientContext = new ClientContext() { EndPoint = endPoint };
        Assert.Equal(endPoint, clientContext.EndPoint);
    }

    [Fact]
    public async Task Open_SetEndPoint_FailTestAsynx()
    {
        var endPoint1 = _serverContext.EndPoint;
        using var endPoint2 = new RandomEndPoint();
        var clientContext = new ClientContext() { EndPoint = endPoint1 };
        await clientContext.OpenAsync(cancellationToken: default);
        Assert.Throws<InvalidOperationException>(() => clientContext.EndPoint = endPoint2);
    }

    [Fact]
    public void Id_Test()
    {
        var clientContext = new ClientContext();
        Assert.NotEqual(Guid.Empty, clientContext.Id);
    }

    [Fact]
    public async Task Open_Close_TestAsync()
    {
        var endPoint = _serverContext.EndPoint;
        var clientContext = new ClientContext() { EndPoint = endPoint };
        Assert.Equal(ServiceState.None, clientContext.ServiceState);
        var token = await clientContext.OpenAsync(cancellationToken: default);
        Assert.Equal(ServiceState.Open, clientContext.ServiceState);
        await clientContext.CloseAsync(token, cancellationToken: default);
        Assert.Equal(ServiceState.None, clientContext.ServiceState);
    }

    [Fact]
    public async Task Open_CloseWithInvalidToken_FailTestAsync()
    {
        var endPoint = _serverContext.EndPoint;
        var clientContext = new ClientContext() { EndPoint = endPoint };
        var token = await clientContext.OpenAsync(cancellationToken: default);
        Assert.Equal(ServiceState.Open, clientContext.ServiceState);
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await clientContext.CloseAsync(Guid.Empty, cancellationToken: default));

        await clientContext.ReleaseAsync(token);
    }

    [Fact]
    public async Task Open_Cancel_Abort_TestAsync()
    {
        var endPoint = _serverContext.EndPoint;
        var clientContext = new ClientContext() { EndPoint = endPoint };
        var cancellationTokenSource = new CancellationTokenSource(millisecondsDelay: 0);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => clientContext.OpenAsync(cancellationTokenSource.Token));
        Assert.Equal(ServiceState.Faulted, clientContext.ServiceState);
        await clientContext.AbortAsync();
        Assert.Equal(ServiceState.None, clientContext.ServiceState);
    }

    [Fact]
    public async Task Open_Open_FailTestAsync()
    {
        var endPoint = _serverContext.EndPoint;
        var clientContext = new ClientContext() { EndPoint = endPoint };
        var token = await clientContext.OpenAsync(cancellationToken: default);
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await clientContext.OpenAsync(cancellationToken: default));
        Assert.Equal(ServiceState.Open, clientContext.ServiceState);

        await clientContext.ReleaseAsync(token);
    }

    [Fact]
    public async Task Open_Close_Cancel_Abort_TestAsync()
    {
        var endPoint = _serverContext.EndPoint;
        var clientContext = new ClientContext() { EndPoint = endPoint };
        var cancellationTokenSource = new CancellationTokenSource(millisecondsDelay: 0);
        var token = await clientContext.OpenAsync(cancellationToken: default);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => clientContext.CloseAsync(token, cancellationTokenSource.Token));
        Assert.Equal(ServiceState.Faulted, clientContext.ServiceState);
        await clientContext.AbortAsync();
        Assert.Equal(ServiceState.None, clientContext.ServiceState);
    }

    [Fact]
    public async Task Open_Close_Close_FailTestAsync()
    {
        var endPoint = _serverContext.EndPoint;
        var clientContext = new ClientContext() { EndPoint = endPoint };
        var token = await clientContext.OpenAsync(cancellationToken: default);
        var task = clientContext.CloseAsync(token, cancellationToken: default);
        await Assert.ThrowsAnyAsync<InvalidOperationException>(
            () => clientContext.CloseAsync(token, cancellationToken: default));
        await task;
        Assert.Equal(ServiceState.None, clientContext.ServiceState);
    }

    [Fact]
    public async Task Opened_TestAsync()
    {
        var endPoint = _serverContext.EndPoint;
        var clientContext = new ClientContext() { EndPoint = endPoint };
        var token = Guid.Empty;
        var cancellationTokenSource = new CancellationTokenSource(millisecondsDelay: 5000);
        var result = await EventTestUtility.RaisesAsync(
            h => clientContext.Opened += h,
            h => clientContext.Opened -= h,
            async () => token = await clientContext.OpenAsync(cancellationTokenSource.Token));

        Assert.True(result);
        await clientContext.ReleaseAsync(token);
    }

    [Fact]
    public async Task Closed_TestAsync()
    {
        var endPoint = _serverContext.EndPoint;
        var clientContext = new ClientContext() { EndPoint = endPoint };
        var cancellationTokenSource = new CancellationTokenSource(millisecondsDelay: 5000);
        var token = await clientContext.OpenAsync(cancellationToken: default);
        var result = await EventTestUtility.RaisesAsync(
            h => clientContext.Closed += h,
            h => clientContext.Closed -= h,
            async () => await clientContext.CloseAsync(token, cancellationTokenSource.Token));

        Assert.True(result);
    }

    [Fact]
    public async Task Faulted_TestAsync()
    {
        var endPoint = _serverContext.EndPoint;
        var clientContext = new ClientContext() { EndPoint = endPoint };
        var cancellationTokenSource = new CancellationTokenSource(millisecondsDelay: 0);
        var result = await EventTestUtility.RaisesAsync(
            h => clientContext.Faulted += h,
            h => clientContext.Faulted -= h,
            async () =>
            {
                try
                {
                    await clientContext.OpenAsync(cancellationTokenSource.Token);
                }
                catch
                {
                }
            });

        Assert.True(result);
    }

    [Fact]
    public async Task Disconnected_TestAsync()
    {
        var endPoint = _serverContext.EndPoint;
        var clientContext = new ClientContext() { EndPoint = endPoint };
        await clientContext.OpenAsync(cancellationToken: default);
        var result = await EventTestUtility.RaisesAsync(
            h => clientContext.Disconnected += h,
            h => clientContext.Disconnected -= h,
            async () =>
            {
                try
                {
                    await _serverContext.CloseAsync(_token, cancellationToken: default);
                }
                catch
                {
                }
            });

        Assert.True(result);
    }

    [Fact]
    public async Task ServiceStateChanged_TestAsync()
    {
        var endPoint = _serverContext.EndPoint;
        var clientContext = new ClientContext() { EndPoint = endPoint };
        var token = Guid.Empty;
        var result = await EventTestUtility.RaisesAsync(
            h => clientContext.ServiceStateChanged += h,
            h => clientContext.ServiceStateChanged -= h,
            async () => token = await clientContext.OpenAsync(cancellationToken: default));

        Assert.True(result);
        await clientContext.ReleaseAsync(token);
    }

    public async Task InitializeAsync()
    {
        _token = await _serverContext.OpenAsync(cancellationToken: default);
    }

    public async Task DisposeAsync()
    {
        if (_serverContext.ServiceState == ServiceState.Open)
        {
            await _serverContext.CloseAsync(_token, cancellationToken: default);
        }
        _endPoint.Dispose();
    }
}
