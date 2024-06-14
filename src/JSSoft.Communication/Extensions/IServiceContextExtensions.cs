// <copyright file="IServiceContextExtensions.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;
using System.Threading.Tasks;
using JSSoft.Communication.Logging;

namespace JSSoft.Communication.Extensions;

public static class IServiceContextExtensions
{
    public static void Debug(this IServiceContext @this, string message)
        => LogUtility.Debug($"{@this} {message}");

    public static void Error(this IServiceContext @this, string message)
        => LogUtility.Error($"{@this} {message}");

    /// <summary>
    /// Close the service context without exception throwing.
    /// </summary>
    /// <param name="this">Indicates a service context instance to close.</param>
    /// <param name="token">Indicates a token to close. you can get the token when
    /// the service context is opened.
    /// </param>
    /// <returns>
    /// If the service context is closed successfully, it returns 0.
    /// If the service context is faulted, it returns 1.
    /// </returns>
    public static async Task<int> ReleaseAsync(this IServiceContext @this, Guid token)
    {
        if (@this.ServiceState == ServiceState.Open)
        {
            try
            {
                await @this.CloseAsync(token, cancellationToken: default);
            }
            catch
            {
                // do nothing
            }
        }

        if (@this.ServiceState == ServiceState.Faulted)
        {
            await @this.AbortAsync();
            return 1;
        }

        return 0;
    }
}
