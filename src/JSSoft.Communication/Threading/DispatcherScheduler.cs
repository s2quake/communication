// <copyright file="DispatcherScheduler.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace JSSoft.Communication.Threading;

public sealed class DispatcherScheduler : TaskScheduler
{
    private readonly CancellationToken _cancellationToken;
    private readonly ConcurrentQueue<Task> _taskQueue = [];
    private readonly ManualResetEvent _executionEventSet = new(false);
    private bool _isRunning = true;
    private bool _isClosed;

    internal DispatcherScheduler(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
    }

    internal void WaitClose()
    {
        if (_isClosed == true)
        {
            throw new InvalidOperationException("Dispatcher is already closed.");
        }

        while (_isRunning == true)
        {
            try
            {
                _executionEventSet.Set();
            }
            catch
            {
                _executionEventSet.Close();
            }

            Thread.Sleep(1);
        }

        _executionEventSet.Dispose();
        _isClosed = true;
    }

    internal void Run()
    {
        try
        {
            while (_cancellationToken.IsCancellationRequested != true)
            {
                if (_taskQueue.TryDequeue(out var task) == true)
                {
                    TryExecuteTask(task);
                }
                else
                {
                    try
                    {
                        _executionEventSet.Reset();
                        _executionEventSet.WaitOne(1000);
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                }
            }

            if (_isClosed == true)
            {
                throw new InvalidOperationException("Dispatcher is already closed.");
            }
        }
        finally
        {
            _isRunning = false;
        }
    }

    protected override IEnumerable<Task> GetScheduledTasks() => _taskQueue;

    protected override void QueueTask(Task task)
    {
        if (_cancellationToken.IsCancellationRequested != true)
        {
            _taskQueue.Enqueue(task);
            _executionEventSet.Set();
        }
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
        return false;
    }
}
