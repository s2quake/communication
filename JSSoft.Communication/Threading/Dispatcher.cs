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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
#if DEBUG
using System.Diagnostics;
#endif

namespace JSSoft.Communication.Threading;

public class Dispatcher : IDisposable
{
    private static readonly Dictionary<Thread, Dispatcher> _dispatcherByThread = [];
    private readonly TaskFactory _factory;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly DispatcherSynchronizationContext _context;
    private readonly DispatcherScheduler _scheduler;
    private bool _isDisposed;

#if DEBUG
    private readonly StackTrace? _stackTrace;
#endif
    private Dispatcher(Thread thread)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _scheduler = new DispatcherScheduler(this, _cancellationTokenSource.Token);
        _factory = new TaskFactory(_cancellationTokenSource.Token, TaskCreationOptions.None, TaskContinuationOptions.None, _scheduler);
        _context = new DispatcherSynchronizationContext(_factory);
        Thread = thread;
        Owner = new object();
    }

    public Dispatcher(object owner)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _scheduler = new DispatcherScheduler(this, _cancellationTokenSource.Token);
        _factory = new TaskFactory(_cancellationTokenSource.Token, TaskCreationOptions.None, TaskContinuationOptions.None, _scheduler);
        _context = new DispatcherSynchronizationContext(_factory);
        Owner = owner;
#if DEBUG
        _stackTrace = new StackTrace(true);
#endif  
        Thread = new Thread(_scheduler.Run)
        {
            Name = $"{owner}: {owner.GetHashCode()}",
            IsBackground = true,
        };
        Thread.Start();
    }

    public override string ToString()
    {
        return $"{Owner}";
    }

    public void VerifyAccess()
    {
        if (!CheckAccess())
        {
            throw new InvalidOperationException("ThreadCannotAccess");
        }
    }

    public bool CheckAccess()
    {
        return Thread == Thread.CurrentThread;
    }

    public void Invoke(Action action)
    {
        if (CheckAccess() == true)
        {
            action();
        }
        else
        {
            var task = _factory.StartNew(action);
            task.Wait();
        }
    }

    public async Task InvokeAsync(Task task)
    {
        try
        {
            task.Start(_scheduler);
            await task;
        }
        catch (TaskSchedulerException e)
        {
            RaiseUnhandledExceptionEvent(e);
        }
    }

    public async Task<TResult> InvokeAsync<TResult>(Task<TResult> task)
    {
        task.Start(_scheduler);
        return await task;
    }

    public async Task InvokeAsync(Action action)
    {
        try
        {
            await _factory.StartNew(action);
        }
        catch (TaskSchedulerException e)
        {
            RaiseUnhandledExceptionEvent(e);
        }
    }

    public TResult Invoke<TResult>(Func<TResult> callback)
    {
        if (CheckAccess() == true)
        {
            return callback();
        }
        else
        {
            var task = _factory.StartNew(callback);
            task.Wait();
            return task.Result;
        }
    }

    public async Task<TResult> InvokeAsync<TResult>(Func<TResult> callback)
    {
        return await _factory.StartNew(callback);
    }

    public void Dispose()
    {
        if (_isDisposed == true)
            throw new ObjectDisposedException($"{this}");
        if (Owner == null)
            throw new InvalidOperationException("IndestructibleObject");
        if (_cancellationTokenSource.IsCancellationRequested == true)
            throw new OperationCanceledException();

        _cancellationTokenSource.Cancel();
        _scheduler.WaitClose();
        _cancellationTokenSource.Dispose();
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }

    public string Name => $"{Owner}";

    public object Owner { get; }

    public Thread Thread { get; }

    public SynchronizationContext SynchronizationContext => _context;

    public static Dispatcher Current
    {
        get
        {
            var thread = Thread.CurrentThread;
            if (_dispatcherByThread.ContainsKey(thread) == false)
            {
                _dispatcherByThread.Add(thread, new Dispatcher(thread));
            }
            return _dispatcherByThread[thread];
        }
    }

#if DEBUG
    internal string StackTrace => $"{_stackTrace}";
#endif

    internal void RaiseUnhandledExceptionEvent(TaskSchedulerException e)
    {

    }
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
        _factory.StartNew(() => d(state));
    }

    public override void Post(SendOrPostCallback d, object? state)
    {
        _factory.StartNew(() => d(state));
    }
}
