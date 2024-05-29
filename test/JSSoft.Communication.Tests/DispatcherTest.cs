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
