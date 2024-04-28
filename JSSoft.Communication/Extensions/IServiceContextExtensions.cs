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
using System.Threading.Tasks;
using JSSoft.Communication.Logging;

namespace JSSoft.Communication.Extensions;

public static class IServiceContextExtensions
{
    public static void Debug(this IServiceContext @this, string message)
        => LogUtility.Debug($"{@this} {message}");

    public static void Error(this IServiceContext @this, string message)
        => LogUtility.Error($"{@this} {message}");

    public static async Task ReleaseAsync(this IServiceContext @this, Guid token)
    {
        if (@this.ServiceState == ServiceState.Open)
        {
            try
            {
                await @this.CloseAsync(token, cancellationToken: default);
            }
            catch
            {
            }
        }
        if (@this.ServiceState == ServiceState.Faulted)
        {
            await @this.AbortAsync();
        }
    }
}
