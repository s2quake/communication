// <copyright file="ServiceContextExtensions.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

namespace JSSoft.Communication.Tests.Extensions;

internal static class ServiceContextExtensions
{
    public static async Task ReleaseAsync(this IServiceContext @this, Guid token)
    {
        if (@this.ServiceState == ServiceState.Open)
        {
            try
            {
                await @this.CloseAsync(token, CancellationToken.None);
            }
            catch
            {
                // do nothing
            }
        }

        if (@this.ServiceState == ServiceState.Faulted)
        {
            await @this.AbortAsync();
        }
    }
}
