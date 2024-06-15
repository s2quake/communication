// <copyright file="IServiceContext.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace JSSoft.Communication;

public interface IServiceContext : IServiceProvider
{
    event EventHandler? Opened;

    event EventHandler? Closed;

    event EventHandler? Faulted;

    event EventHandler? Disconnected;

    event EventHandler? ServiceStateChanged;

    IReadOnlyDictionary<string, IService> Services { get; }

    EndPoint EndPoint { get; set; }

    Guid Id { get; }

    ServiceState ServiceState { get; }

    Task<Guid> OpenAsync(CancellationToken cancellationToken);

    Task CloseAsync(Guid token, CancellationToken cancellationToken);

    Task AbortAsync();
}
