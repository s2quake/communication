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

public sealed class ServerContextTest
{
    private const int Timeout = 30000;

    public interface ITestService1 : IService
    {
    }

    public interface ITestService2 : IService
    {
    }

    sealed class TestService1 : ServerService<ITestService1>, ITestService1
    {
    }

    sealed class TestService2 : ServerService<ITestService2>, ITestService2
    {
    }

    [Fact]
    public void Constructor_Test()
    {
        var serverContext0 = new ServerContext();
        Assert.Empty(serverContext0.Services);
        var serverContext1 = new ServerContext(services:
        [
            new TestService2(),
        ]);
        Assert.Single(serverContext1.Services);
        var serverContext2 = new ServerContext(services:
        [
            new TestService1(),
            new TestService2(),
        ]);
        Assert.Equal(2, serverContext2.Services.Count);
    }

    [Fact]
    public void Constructor_WithSameTypeServices_FailTest()
    {
        var services = new IService[] { new TestService1(), new TestService1() };
        Assert.Throws<ArgumentException>(() => new ServerContext(services));
    }

    [Fact]
    public void EndPoint_Test()
    {
        var endPoint = EndPointUtility.GetEndPoint();
        var serverContext = new ServerContext() { EndPoint = endPoint };
        Assert.Equal(endPoint, serverContext.EndPoint);
    }

    [Fact]
    public async Task Open_SetEndPoint_FailTestAsynx()
    {
        var endPoint1 = EndPointUtility.GetEndPoint();
        var endPoint2 = EndPointUtility.GetEndPoint();
        var serverContext = new ServerContext() { EndPoint = endPoint1 };
        await serverContext.OpenAsync(cancellationToken: default);
        Assert.Throws<InvalidOperationException>(() => serverContext.EndPoint = endPoint2);
    }

    [Fact]
    public void Id_Test()
    {
        var serverContext = new ServerContext();
        Assert.NotEqual(Guid.Empty, serverContext.Id);
    }

    [Fact]
    public async Task Open_Close_TestAsync()
    {
        var endPoint = EndPointUtility.GetEndPoint();
        var serverContext = new ServerContext() { EndPoint = endPoint };
        Assert.Equal(ServiceState.None, serverContext.ServiceState);
        var token = await serverContext.OpenAsync(cancellationToken: default);
        Assert.Equal(ServiceState.Open, serverContext.ServiceState);
        await serverContext.CloseAsync(token, cancellationToken: default);
        Assert.Equal(ServiceState.None, serverContext.ServiceState);
    }

    [Fact]
    public async Task Open_CloseWithInvalidToken_FailTestAsync()
    {
        var endPoint = EndPointUtility.GetEndPoint();
        var serverContext = new ServerContext() { EndPoint = endPoint };
        var token = await serverContext.OpenAsync(cancellationToken: default);
        Assert.Equal(ServiceState.Open, serverContext.ServiceState);
        await Assert.ThrowsAsync<ArgumentException>(
            () => serverContext.CloseAsync(Guid.Empty, cancellationToken: default));
    }

    [Fact]
    public async Task Open_Cancel_Abort_TestAsync()
    {
        var endPoint = EndPointUtility.GetEndPoint();
        var serverContext = new ServerContext() { EndPoint = endPoint };
        var cancellationTokenSource = new CancellationTokenSource(millisecondsDelay: 0);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => serverContext.OpenAsync(cancellationTokenSource.Token));
        Assert.Equal(ServiceState.Faulted, serverContext.ServiceState);
        await serverContext.AbortAsync();
        Assert.Equal(ServiceState.None, serverContext.ServiceState);
    }

    [Fact]
    public async Task Open_Open_FailTestAsync()
    {
        var endPoint = EndPointUtility.GetEndPoint();
        var serverContext = new ServerContext() { EndPoint = endPoint };
        await serverContext.OpenAsync(cancellationToken: default);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => serverContext.OpenAsync(cancellationToken: default));
        Assert.Equal(ServiceState.Open, serverContext.ServiceState);
    }

    [Fact]
    public async Task Open_Close_Cancel_Abort_TestAsync()
    {
        var endPoint = EndPointUtility.GetEndPoint();
        var serverContext = new ServerContext() { EndPoint = endPoint };
        var cancellationTokenSource = new CancellationTokenSource(millisecondsDelay: 0);
        var token = await serverContext.OpenAsync(cancellationToken: default);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => serverContext.CloseAsync(token, cancellationTokenSource.Token));
        Assert.Equal(ServiceState.Faulted, serverContext.ServiceState);
        await serverContext.AbortAsync();
        Assert.Equal(ServiceState.None, serverContext.ServiceState);
    }

    [Fact]
    public async Task Open_Close_Close_FailTestAsync()
    {
        var endPoint = EndPointUtility.GetEndPoint();
        var serverContext = new ServerContext() { EndPoint = endPoint };
        var token = await serverContext.OpenAsync(cancellationToken: default);
        var task = serverContext.CloseAsync(token, cancellationToken: default);
        await Assert.ThrowsAnyAsync<InvalidOperationException>(
            () => serverContext.CloseAsync(token, cancellationToken: default));
        await task;
        Assert.Equal(ServiceState.None, serverContext.ServiceState);
    }

    [Fact]
    public async Task Opened_TestAsync()
    {
        var endPoint = EndPointUtility.GetEndPoint();
        var serverContext = new ServerContext() { EndPoint = endPoint };
        var cancellationTokenSource = new CancellationTokenSource(millisecondsDelay: Timeout);
        var result = await EventTestUtility.RaisesAsync(
            h => serverContext.Opened += h,
            h => serverContext.Opened -= h,
            () => serverContext.OpenAsync(cancellationTokenSource.Token));

        Assert.True(result);
    }

    [Fact]
    public async Task Closed_TestAsync()
    {
        var endPoint = EndPointUtility.GetEndPoint();
        var serverContext = new ServerContext() { EndPoint = endPoint };
        var cancellationTokenSource = new CancellationTokenSource(millisecondsDelay: Timeout);
        var token = await serverContext.OpenAsync(cancellationToken: default);
        var result = await EventTestUtility.RaisesAsync(
            h => serverContext.Closed += h,
            h => serverContext.Closed -= h,
            () => serverContext.CloseAsync(token, cancellationTokenSource.Token));

        Assert.True(result);
    }

    [Fact]
    public async Task Faulted_TestAsync()
    {
        var endPoint = EndPointUtility.GetEndPoint();
        var serverContext = new ServerContext() { EndPoint = endPoint };
        var cancellationTokenSource = new CancellationTokenSource(millisecondsDelay: 0);
        var result = await EventTestUtility.RaisesAsync(
            h => serverContext.Faulted += h,
            h => serverContext.Faulted -= h,
            async () =>
            {
                try
                {
                    await serverContext.OpenAsync(cancellationTokenSource.Token);
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
        var endPoint = EndPointUtility.GetEndPoint();
        var serverContext = new ServerContext() { EndPoint = endPoint };
        var result = await EventTestUtility.RaisesAsync(
            h => serverContext.ServiceStateChanged += h,
            h => serverContext.ServiceStateChanged -= h,
            () => serverContext.OpenAsync(cancellationToken: default));

        Assert.True(result);
    }
}
