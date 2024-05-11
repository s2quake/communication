namespace JSSoft.Communication.Tests;

public class InvokeTest : ClientTestBase<InvokeTest.ITestService, InvokeTest.TestServer>
{
    public InvokeTest()
        : base(new TestServer())
    {
    }

    public interface ITestService
    {
        [ServerMethod]
        void Invoke();

        [ServerMethod(IsOneWay = true)]
        void InvokeOneWay();

        [ServerMethod]
        int InvokeAndReturn();

        [ServerMethod]
        Task InvokeAsync();

        [ServerMethod]
        Task<int> InvokeAndReturnAsync();

        [ServerMethod]
        Task InvokeAsync(CancellationToken cancellationToken);

        [ServerMethod]
        Task<int> InvokeAndReturnAsync(CancellationToken cancellationToken);
    }

    public sealed class TestServer : ServerService<ITestService>, ITestService
    {
        public object? Result { get; set; }

        public void Invoke()
        {
            Result = nameof(Invoke);
        }

        public void InvokeOneWay()
        {
            Result = default;
            Thread.Sleep(100);
            Result = nameof(InvokeOneWay);
        }

        public int InvokeAndReturn()
        {
            Result = nameof(InvokeAndReturn);
            return 1;
        }

        public Task InvokeAsync()
        {
            Result = nameof(InvokeAsync);
            return Task.CompletedTask;
        }

        public Task<int> InvokeAndReturnAsync()
        {
            Result = nameof(InvokeAndReturnAsync);
            return Task.Run(() => 2);
        }

        public Task InvokeAsync(CancellationToken cancellationToken)
        {
            Result = nameof(InvokeAsync) + nameof(CancellationToken);
            return Task.CompletedTask;
        }

        public Task<int> InvokeAndReturnAsync(CancellationToken cancellationToken)
        {
            Result = nameof(InvokeAndReturnAsync) + nameof(CancellationToken);
            return Task.Run(() => 3);
        }
    }

    [Fact]
    public void Invoke_Test()
    {
        Client.Invoke();
        Assert.Equal(nameof(Client.Invoke), ServerService.Result);
    }

    [Fact]
    public void InvokeOneWay_Test()
    {
        Client.InvokeOneWay();
        Assert.NotEqual(nameof(Client.InvokeOneWay), ServerService.Result);
    }

    [Fact]
    public void InvokeAndReturn_Test()
    {
        var actualValue = Client.InvokeAndReturn();
        Assert.Equal(nameof(Client.InvokeAndReturn), ServerService.Result);
        Assert.Equal(1, actualValue);
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
        Assert.Equal(nameof(Client.InvokeAndReturnAsync) + nameof(CancellationToken), ServerService.Result);
        Assert.Equal(3, actualValue);
    }
}
