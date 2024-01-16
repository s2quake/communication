using JSSoft.Communication.Threading;

namespace JSSoft.Communication.Tests;

public class DispatcherTest
{
    [Fact]
    public void Invoke_Test()
    {
        var dispatcher = new Dispatcher(new());
        var b = false;
        dispatcher.Invoke(() => b = true);
        Assert.True(b);
    }

    [Fact]
    public void Invoke_FailTest()
    {
        var dispatcher = new Dispatcher(new());
        dispatcher.Dispose();
        Assert.Throws<ObjectDisposedException>(() =>
        {
            dispatcher.Invoke(() => { });
        });
    }

    [Fact]
    public void InvokeGeneric_Test()
    {
        var dispatcher = new Dispatcher(new());
        var v = dispatcher.Invoke(() => 0);
        Assert.Equal(0, v);
    }

    [Fact]
    public void InvokeGeneric_FailTest()
    {
        var dispatcher = new Dispatcher(new());
        dispatcher.Dispose();
        Assert.Throws<ObjectDisposedException>(() =>
        {
            dispatcher.Invoke(() => 0);
        });
    }

    [Fact]
    public async Task InvokeAsync_Test()
    {
        var dispatcher = new Dispatcher(new());
        var b = false;
        await dispatcher.InvokeAsync(() => b = true);
        Assert.True(b);
    }

    [Fact]
    public async Task InvokeAsync_FailTest()
    {
        var dispatcher = new Dispatcher(new());
        dispatcher.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
        {
            await dispatcher.InvokeAsync(() => { });
        });
    }

    [Fact]
    public async Task InvokeAsync_FailTest2()
    {
        var dispatcher = new Dispatcher(new());
        dispatcher.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
        {
            await dispatcher.InvokeAsync(() => { });
        });
    }

    [Fact]
    public async Task InvokeGenericAsync_Test()
    {
        var dispatcher = new Dispatcher(new());
        var v = await dispatcher.InvokeAsync(() => 1);
        Assert.Equal(1, v);
    }

    [Fact]
    public async Task InvokeGenericAsync_FailTest()
    {
        var dispatcher = new Dispatcher(new());
        dispatcher.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
        {
            await dispatcher.InvokeAsync(() => 1);
        });
    }

    [Fact]
    public void InvokeGenericAsync_WaitTest()
    {
        var dispatcher = new Dispatcher(new());
        var b = false;
        dispatcher.InvokeAsync(() =>
        {
            Thread.Sleep(1000);
            b = true;
        });
        Thread.Sleep(10);
        dispatcher.Dispose();
        Assert.True(b);
    }

    [Fact]
    public void Post_Test()
    {
        var dispatcher = new Dispatcher(new());
        var manualResetEvent = new ManualResetEvent(initialState: false);
        dispatcher.Post(() =>
        {
            manualResetEvent.Set();
        });
        var b = manualResetEvent.WaitOne(millisecondsTimeout: 1000);
        Assert.True(b);
    }

    [Fact]
    public void Post_FailTest()
    {
        var dispatcher = new Dispatcher(new());
        dispatcher.Dispose();
        Assert.Throws<ObjectDisposedException>(() =>
        {
            dispatcher.Post(() => { });
        });
    }
}
