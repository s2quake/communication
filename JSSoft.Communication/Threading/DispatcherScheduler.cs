// Released under the MIT License.
// 
// Copyright (c) 2018 Ntreev Soft co., Ltd.
// Copyright (c) 2020 Jeesu Choi
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit
// persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the
// Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 
// Forked from https://github.com/NtreevSoft/Ntreev.Library
// Namespaces and files starting with "Ntreev" have been renamed to "JSSoft".

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace JSSoft.Communication.Threading;

public sealed class DispatcherScheduler : TaskScheduler
{
    private readonly Dispatcher _dispatcher;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly ConcurrentQueue<Task> _taskQueue = [];
    private readonly ManualResetEvent _executionEventSet = new(false);
    private bool _isRunning = true;
    private bool _isClosed;

    internal DispatcherScheduler(Dispatcher dispatcher, CancellationTokenSource cancellationTokenSource)
    {
        _dispatcher = dispatcher;
        _cancellationTokenSource = cancellationTokenSource;
    }

    public int ProcessAll()
    {
        return Process(int.MaxValue);
    }

    public int Process(int milliseconds)
    {
        _dispatcher.VerifyAccess();
        if (_isRunning == true)
            throw new InvalidOperationException("SchedulerIsAlreadyRunning");

        var dateTime = DateTime.Now;
        var completion = 0;
        var count = _taskQueue.Count;
        while (_taskQueue.TryDequeue(out var task))
        {
            TryExecuteTask(task);
            completion++;
            var span = DateTime.Now - dateTime;
            if (span.TotalMilliseconds > milliseconds || completion >= count)
                break;
        }
        return completion;
    }

    public bool ProcessOnce()
    {
        _dispatcher.VerifyAccess();
        if (_isRunning == true)
            throw new InvalidOperationException("SchedulerIsAlreadyRunning");

        if (_taskQueue.TryDequeue(out var task) == true)
        {
            TryExecuteTask(task);
        }
        return _taskQueue.Count != 0;
    }

    protected override IEnumerable<Task> GetScheduledTasks() => _taskQueue;

    protected override void QueueTask(Task task)
    {
        if (_cancellationTokenSource.IsCancellationRequested == true)
            throw new InvalidOperationException();

        _taskQueue.Enqueue(task);
        _executionEventSet.Set();
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
        return false;
    }

    internal void WaitClose()
    {
        if (_isClosed == true)
            throw new InvalidOperationException();

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
#if DEBUG
            var owner = _dispatcher.Owner;
            var stackTrace = _dispatcher.StackTrace;
#endif

            while (_cancellationTokenSource.IsCancellationRequested != true)
            {
                if (_taskQueue.TryDequeue(out var task) == true)
                {
                    TryExecuteTask(task);
                }
                else
                {
                    try
                    {
                        _executionEventSet.WaitOne();
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                }
            }
            if (_isClosed == true)
                throw new InvalidOperationException();
        }
        finally
        {
            _isRunning = false;
        }
    }
}
