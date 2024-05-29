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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace JSSoft.Communication.Threading;

public sealed class DispatcherScheduler : TaskScheduler
{
    private readonly Dispatcher _dispatcher;
    private readonly CancellationToken _cancellationToken;
    private readonly ConcurrentQueue<Task> _taskQueue = [];
    private readonly ManualResetEvent _executionEventSet = new(false);
    private bool _isRunning = true;
    private bool _isClosed;

    internal DispatcherScheduler(Dispatcher dispatcher, CancellationToken cancellationToken)
    {
        _dispatcher = dispatcher;
        _cancellationToken = cancellationToken;
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

    internal void WaitClose()
    {
        if (_isClosed == true)
            throw new InvalidOperationException("Dispatcher is already closed.");

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
                throw new InvalidOperationException("Dispatcher is already closed.");
        }
        finally
        {
            _isRunning = false;
        }
    }
}
