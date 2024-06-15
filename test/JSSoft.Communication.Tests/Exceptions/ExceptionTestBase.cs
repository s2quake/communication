// <copyright file="ExceptionTestBase.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

namespace JSSoft.Communication.Tests.Exceptions;

public abstract class ExceptionTestBase<TException>
    : ClientTestBase<ExceptionTestBase<TException>.ITestService>
    where TException : Exception
{
    protected ExceptionTestBase()
        : base(new TestServer())
    {
    }

    public interface ITestService
    {
        void Invoke()
            => throw (TException)Activator.CreateInstance(typeof(TException), nameof(Invoke))!;

        Task InvokeAsync()
            => throw (TException)Activator.CreateInstance(typeof(TException), nameof(Invoke))!;

        Task<int> InvokeAndReturnAsync()
            => throw (TException)Activator.CreateInstance(typeof(TException), nameof(Invoke))!;
    }

    [Fact]
    public void Invoke_Test()
    {
        var b = true;
        try
        {
            Client.Invoke();
        }
        catch
        {
            b = false;
        }

        Assert.True(b);
    }

    [Fact]
    public async Task InvokeAsync_Test()
    {
        await Assert.ThrowsAsync<TException>(() => Client.InvokeAsync());
    }

    [Fact]
    public async Task InvokeAndReturnAsync_Test()
    {
        await Assert.ThrowsAsync<TException>(() => Client.InvokeAndReturnAsync());
    }

    [Fact]
    public async Task InvokeAsyncWithCancellation_Test()
    {
        await Assert.ThrowsAsync<TException>(() => Client.InvokeAsync());
    }

    [Fact]
    public async Task InvokeAndReturnAsyncWithCancellation_Test()
    {
        await Assert.ThrowsAsync<TException>(() => Client.InvokeAndReturnAsync());
    }

    private sealed class TestServer : ServerService<ITestService>, ITestService
    {
    }
}
