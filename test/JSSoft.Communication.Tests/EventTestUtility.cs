// <copyright file="EventTestUtility.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

namespace JSSoft.Communication.Tests;

public static class EventTestUtility
{
    public static async Task<bool> RaisesAsync(
        Action<EventHandler> attach, Action<EventHandler> detach, Func<Task> testCode)
    {
        using var manualResetEvent = new ManualResetEvent(initialState: false);
        attach(Handler);
        try
        {
            await testCode();
        }
        catch
        {
            return false;
        }
        finally
        {
            detach(Handler);
        }

        return manualResetEvent.WaitOne(millisecondsTimeout: 10000);

        void Handler(object? s, EventArgs args)
        {
            manualResetEvent.Set();
        }
    }
}
