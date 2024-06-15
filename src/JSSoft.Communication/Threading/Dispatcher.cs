// <copyright file="Dispatcher.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

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
#if DEBUG
    private readonly System.Diagnostics.StackTrace _stackTrace;
#endif

    public Dispatcher(object owner)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _cancellationToken = _cancellationTokenSource.Token;
        _scheduler = new DispatcherScheduler(_cancellationToken);
        _factory = new TaskFactory(
            _cancellationToken, TaskCreationOptions.None, TaskContinuationOptions.None, _scheduler);
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

#if DEBUG
    internal string StackTrace => $"{_stackTrace}";
#endif

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
        ObjectDisposedException.ThrowIf(_cancellationToken.IsCancellationRequested, this);

        if (CheckAccess() == true)
        {
            action();
            return;
        }

        var task = _factory.StartNew(action, _cancellationToken);
        task.Wait(_cancellationToken);
    }

    public TResult Invoke<TResult>(Func<TResult> func)
    {
        ObjectDisposedException.ThrowIf(_cancellationToken.IsCancellationRequested, this);

        if (CheckAccess() == true)
        {
            return func();
        }

        var task = _factory.StartNew(func, _cancellationToken);
        task.Wait(_cancellationToken);
        return task.Result;
    }

    public void Post(Action action)
    {
        ObjectDisposedException.ThrowIf(_cancellationToken.IsCancellationRequested, this);

        _factory.StartNew(action, _cancellationToken);
    }

    public async Task<TResult> InvokeAsync<TResult>(Task<TResult> task)
    {
        task.Start(_scheduler);
        return await task;
    }

    public Task InvokeAsync(Action action)
    {
        ObjectDisposedException.ThrowIf(_cancellationToken.IsCancellationRequested, this);

        return _factory.StartNew(action, _cancellationToken);
    }

    public async Task<TResult> InvokeAsync<TResult>(Func<TResult> callback)
    {
        ObjectDisposedException.ThrowIf(_cancellationToken.IsCancellationRequested, this);

        return await _factory.StartNew(callback, _cancellationToken);
    }

    public void Dispose()
    {
        ObjectDisposedException.ThrowIf(_cancellationTokenSource.IsCancellationRequested, this);

        _cancellationTokenSource.Cancel();
        _scheduler.WaitClose();
        _cancellationTokenSource.Dispose();
        GC.SuppressFinalize(this);
    }
}
