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

using System;
using System.Threading;
using System.Threading.Tasks;

namespace JSSoft.Communication.Threading;

public class Dispatcher : IDisposable
{
    private readonly TaskFactory _factory;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly CancellationToken _cancellationToken;
    private readonly DispatcherSynchronizationContext _context;
    private readonly DispatcherScheduler _scheduler;
    private bool _isDisposed;

#if DEBUG
    private readonly System.Diagnostics.StackTrace? _stackTrace;
#endif
    public Dispatcher(object owner)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _cancellationToken = _cancellationTokenSource.Token;
        _scheduler = new DispatcherScheduler(this, _cancellationToken);
        _factory = new TaskFactory(_cancellationToken, TaskCreationOptions.None, TaskContinuationOptions.None, _scheduler);
        _context = new DispatcherSynchronizationContext(_factory);
        Owner = owner;
#if DEBUG
        _stackTrace = new System.Diagnostics.StackTrace(true);
#endif  
        Thread = new Thread(_scheduler.Run)
        {
            Name = $"{owner}: {owner.GetHashCode()}",
            IsBackground = true,
        };
        Thread.Start();
    }

    public string Name => $"{Owner}";

    public object Owner { get; }

    public Thread Thread { get; }

    public SynchronizationContext SynchronizationContext => _context;

    public override string ToString() => $"{Owner}";

    public void VerifyAccess()
    {
        if (!CheckAccess())
        {
            throw new InvalidOperationException("Thread Cannot Access");
        }
    }

    public bool CheckAccess() => Thread == Thread.CurrentThread;

    public void Invoke(Action action)
    {
        if (_cancellationToken.IsCancellationRequested == true)
            throw new ObjectDisposedException($"{this}");

        if (CheckAccess() == true)
        {
            action();
            return;
        }
        var task = _factory.StartNew(action, _cancellationToken);
        task.Wait(_cancellationToken);
    }

    public void Post(Action action)
    {
        if (_cancellationToken.IsCancellationRequested == true)
            throw new ObjectDisposedException($"{this}");

        _factory.StartNew(action, _cancellationToken);
    }

    public async Task<TResult> InvokeAsync<TResult>(Task<TResult> task)
    {
        task.Start(_scheduler);
        return await task;
    }

    public Task InvokeAsync(Action action)
    {
        if (_cancellationToken.IsCancellationRequested == true)
            throw new ObjectDisposedException($"{this}");

        return _factory.StartNew(action, _cancellationToken);
    }

    public TResult Invoke<TResult>(Func<TResult> func)
    {
        if (_cancellationTokenSource.IsCancellationRequested == true)
            throw new ObjectDisposedException($"{this}");

        if (CheckAccess() == true)
        {
            return func();
        }
        var task = _factory.StartNew(func, _cancellationToken);
        task.Wait(_cancellationToken);
        return task.Result;
    }

    public async Task<TResult> InvokeAsync<TResult>(Func<TResult> callback)
    {
        if (_cancellationTokenSource.IsCancellationRequested == true)
            throw new ObjectDisposedException($"{this}");

        return await _factory.StartNew(callback, _cancellationToken);
    }

    public void Dispose()
    {
        if (_isDisposed == true)
            throw new ObjectDisposedException($"{this}");
        if (_cancellationTokenSource.IsCancellationRequested == true)
            throw new ObjectDisposedException($"{this}");

        _cancellationTokenSource.Cancel();
        _scheduler.WaitClose();
        _cancellationTokenSource.Dispose();
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }

#if DEBUG
    internal string StackTrace => $"{_stackTrace}";
#endif
}

public sealed class DispatcherSynchronizationContext : SynchronizationContext
{
    private readonly TaskFactory _factory;

    internal DispatcherSynchronizationContext(TaskFactory factory)
    {
        _factory = factory;
    }

    public override void Send(SendOrPostCallback d, object? state)
    {
        _factory.StartNew(() => d(state)).Wait();
    }

    public override void Post(SendOrPostCallback d, object? state)
    {
        _factory.StartNew(() => d(state));
    }
}
