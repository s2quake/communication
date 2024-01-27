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

namespace JSSoft.Communication.Tests.Exceptions;

public abstract class ExceptionTestBase<TException> : ClientTestBase<ExceptionTestBase<TException>.ITestService>
    where TException : Exception
{
    protected ExceptionTestBase()
        : base(new TestServer())
    {
    }

    public interface ITestService
    {
        [ServerMethod]
        void Invoke() => throw (TException)Activator.CreateInstance(typeof(TException), args: [nameof(Invoke)])!;

        [ServerMethod(IsOneWay = true)]
        void InvokeOneWay() => throw (TException)Activator.CreateInstance(typeof(TException), args: [nameof(Invoke)])!;

        [ServerMethod]
        int InvokeAndReturn() => throw (TException)Activator.CreateInstance(typeof(TException), args: [nameof(Invoke)])!;

        [ServerMethod]
        Task InvokeAsync() => throw (TException)Activator.CreateInstance(typeof(TException), args: [nameof(Invoke)])!;

        [ServerMethod]
        Task<int> InvokeAndReturnAsync() => throw (TException)Activator.CreateInstance(typeof(TException), args: [nameof(Invoke)])!;
    }

    sealed class TestServer : ServerService<ITestService>, ITestService
    {
    }

    [Fact]
    public void Invoke_Test()
    {
        Assert.Throws<TException>(() => Client.Invoke());
    }

    [Fact]
    public void InvokeOneWay_Test()
    {
        Client.InvokeOneWay();
    }

    [Fact]
    public void InvokeAndReturn_Test()
    {
        Assert.Throws<TException>(() => Client.InvokeAndReturn());
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
}
