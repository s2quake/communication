// <copyright file="DispatcherSynchronizationContext.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;

namespace JSSoft.Communication.Threading;

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
