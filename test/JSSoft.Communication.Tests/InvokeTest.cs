// <copyright file="InvokeTest.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

namespace JSSoft.Communication.Tests;

public class InvokeTest : ClientTestBase<InvokeTest.ITestService, InvokeTest.TestServer>
{
    public InvokeTest()
        : base(new TestServer())
    {
    }

    public interface ITestService
    {
        void Invoke();

        Task InvokeAsync();

        Task InvokeAsync(CancellationToken cancellationToken);

        Task<int> InvokeAndReturnAsync();

        Task<int> InvokeAndReturnAsync(CancellationToken cancellationToken);
    }

    [Fact]
    public void Invoke_Test()
    {
        Client.Invoke();
        Assert.Null(ServerService.Result);
    }

    [Fact]
    public async Task InvokeAsync_Test()
    {
        await Client.InvokeAsync();
        Assert.Equal(nameof(Client.InvokeAsync), ServerService.Result);
    }

    [Fact]
    public async Task InvokeAndReturnAsync_Test()
    {
        var actualValue = await Client.InvokeAndReturnAsync();
        Assert.Equal(nameof(Client.InvokeAndReturnAsync), ServerService.Result);
        Assert.Equal(2, actualValue);
    }

    [Fact]
    public async Task InvokeAsyncWithCancellation_Test()
    {
        await Client.InvokeAsync(CancellationToken.None);
        Assert.Equal(nameof(Client.InvokeAsync) + nameof(CancellationToken), ServerService.Result);
    }

    [Fact]
    public async Task InvokeAndReturnAsyncWithCancellation_Test()
    {
        var actualValue = await Client.InvokeAndReturnAsync(CancellationToken.None);
        var expectedResult = nameof(Client.InvokeAndReturnAsync) + nameof(CancellationToken);
        Assert.Equal(expectedResult, ServerService.Result);
        Assert.Equal(3, actualValue);
    }

    public sealed class TestServer : ServerService<ITestService>, ITestService
    {
        public object? Result { get; set; }

        public void Invoke()
        {
            Result = nameof(Invoke);
        }

        public Task InvokeAsync()
        {
            Result = nameof(InvokeAsync);
            return Task.CompletedTask;
        }

        public Task InvokeAsync(CancellationToken cancellationToken)
        {
            Result = nameof(InvokeAsync) + nameof(CancellationToken);
            return Task.CompletedTask;
        }

        public Task<int> InvokeAndReturnAsync()
        {
            Result = nameof(InvokeAndReturnAsync);
            return Task.Run(() => 2);
        }

        public Task<int> InvokeAndReturnAsync(CancellationToken cancellationToken)
        {
            Result = nameof(InvokeAndReturnAsync) + nameof(CancellationToken);
            return Task.Run(() => 3);
        }
    }
}
