// <copyright file="ServerContextTest.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using JSSoft.Communication.Extensions;

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

    [Fact]
    public void Constructor_Test()
    {
        var serverContext0 = new ServerContext();
        Assert.Empty(serverContext0.Services);
        var serverContext1 = new ServerContext(new TestService2());
        Assert.Single(serverContext1.Services);
        var serverContext2 = new ServerContext(new TestService1(), new TestService2());
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
        using var endPoint = new RandomEndPoint();
        var serverContext = new ServerContext() { EndPoint = endPoint };
        Assert.Equal(endPoint, serverContext.EndPoint);
    }

    [Fact]
    public async Task Open_SetEndPoint_FailTestAsynx()
    {
        using var endPoint1 = new RandomEndPoint();
        using var endPoint2 = new RandomEndPoint();
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
        using var endPoint = new RandomEndPoint();
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
        using var endPoint = new RandomEndPoint();
        var serverContext = new ServerContext() { EndPoint = endPoint };
        var token = await serverContext.OpenAsync(cancellationToken: default);
        Assert.Equal(ServiceState.Open, serverContext.ServiceState);
        await Assert.ThrowsAsync<ArgumentException>(
            () => serverContext.CloseAsync(Guid.Empty, cancellationToken: default));

        await serverContext.ReleaseAsync(token);
    }

    [Fact]
    public async Task Open_Cancel_Abort_TestAsync()
    {
        using var endPoint = new RandomEndPoint();
        var serverContext = new ServerContext() { EndPoint = endPoint };
        using var cancellationTokenSource = new CancellationTokenSource(millisecondsDelay: 0);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => serverContext.OpenAsync(cancellationTokenSource.Token));
        Assert.Equal(ServiceState.Faulted, serverContext.ServiceState);
        await serverContext.AbortAsync();
        Assert.Equal(ServiceState.None, serverContext.ServiceState);
    }

    [Fact]
    public async Task Open_Open_FailTestAsync()
    {
        using var endPoint = new RandomEndPoint();
        var serverContext = new ServerContext() { EndPoint = endPoint };
        await serverContext.OpenAsync(cancellationToken: default);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => serverContext.OpenAsync(cancellationToken: default));
        Assert.Equal(ServiceState.Open, serverContext.ServiceState);
    }

    [Fact]
    public async Task Open_Close_Cancel_Abort_TestAsync()
    {
        using var endPoint = new RandomEndPoint();
        var serverContext = new ServerContext() { EndPoint = endPoint };
        using var cancellationTokenSource = new CancellationTokenSource(millisecondsDelay: 0);
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
        using var endPoint = new RandomEndPoint();
        var serverContext = new ServerContext() { EndPoint = endPoint };
        var token = await serverContext.OpenAsync(cancellationToken: default);
        var task = serverContext.CloseAsync(token, cancellationToken: default);
        await Assert.ThrowsAnyAsync<InvalidOperationException>(
            () => serverContext.CloseAsync(token, cancellationToken: default));
        await task;
        Assert.Equal(ServiceState.None, serverContext.ServiceState);
        await serverContext.ReleaseAsync(token);
    }

    [Fact]
    public async Task Opened_TestAsync()
    {
        using var endPoint = new RandomEndPoint();
        var serverContext = new ServerContext() { EndPoint = endPoint };
        var token = Guid.Empty;
        using var cancellationTokenSource = new CancellationTokenSource(millisecondsDelay: Timeout);
        var result = await EventTestUtility.RaisesAsync(
            h => serverContext.Opened += h,
            h => serverContext.Opened -= h,
            async () => token = await serverContext.OpenAsync(cancellationTokenSource.Token));

        Assert.True(result);
        await serverContext.ReleaseAsync(token);
    }

    [Fact]
    public async Task Closed_TestAsync()
    {
        using var endPoint = new RandomEndPoint();
        var serverContext = new ServerContext() { EndPoint = endPoint };
        using var cancellationTokenSource = new CancellationTokenSource(millisecondsDelay: Timeout);
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
        using var endPoint = new RandomEndPoint();
        var serverContext = new ServerContext() { EndPoint = endPoint };
        using var cancellationTokenSource = new CancellationTokenSource(millisecondsDelay: 0);
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
                    // do nothing
                }
            });

        Assert.True(result);
    }

    [Fact]
    public async Task ServiceStateChanged_TestAsync()
    {
        using var endPoint = new RandomEndPoint();
        var serverContext = new ServerContext() { EndPoint = endPoint };
        var token = Guid.NewGuid();
        var result = await EventTestUtility.RaisesAsync(
            h => serverContext.ServiceStateChanged += h,
            h => serverContext.ServiceStateChanged -= h,
            async () => token = await serverContext.OpenAsync(cancellationToken: default));

        Assert.True(result);
        await serverContext.ReleaseAsync(token);
    }

    private sealed class TestService1 : ServerService<ITestService1>, ITestService1
    {
    }

    private sealed class TestService2 : ServerService<ITestService2>, ITestService2
    {
    }
}
