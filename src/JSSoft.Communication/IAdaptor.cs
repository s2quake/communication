// <copyright file="IAdaptor.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace JSSoft.Communication;

public interface IAdaptor : IAsyncDisposable
{
    event EventHandler? Disconnected;

    Task OpenAsync(EndPoint endPoint, CancellationToken cancellationToken);

    Task CloseAsync(CancellationToken cancellationToken);

    void InvokeOneWay(InvokeOptions options);

    void Invoke(InvokeOptions options);

    T Invoke<T>(InvokeOptions options);

    Task InvokeAsync(InvokeOptions options, CancellationToken cancellationToken);

    Task<T> InvokeAsync<T>(InvokeOptions options, CancellationToken cancellationToken);
}
