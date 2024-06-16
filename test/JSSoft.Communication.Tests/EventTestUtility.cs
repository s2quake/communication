// <copyright file="EventTestUtility.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

namespace JSSoft.Communication.Tests;

public static class EventTestUtility
{
    public const int Timeout = 10000;

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

        return manualResetEvent.WaitOne(Timeout);

        void Handler(object? s, EventArgs args)
        {
            manualResetEvent.Set();
        }
    }

    public static async Task<int> RaisesManyAsync<TObject, TEventArgs>(
        TObject[] items,
        Action<TObject, EventHandler<TEventArgs>> attach,
        Action<TObject, EventHandler<TEventArgs>> detach,
        Action testCode)
        where TObject : notnull
        where TEventArgs : EventArgs
    {
        using var eventByItem
            = new EventObjectCollection<TObject, TEventArgs>(items, attach, detach);
        testCode();
        return await eventByItem.WaiyAsync(Timeout);
    }

    public static async Task<int> RaisesManyAsync<TObject, TEventArgs>(
        TObject[] items,
        Action<TObject, EventHandler<TEventArgs>> attach,
        Action<TObject, EventHandler<TEventArgs>> detach,
        Func<Task> testCode)
        where TObject : notnull
        where TEventArgs : EventArgs
    {
        using var eventByItem
            = new EventObjectCollection<TObject, TEventArgs>(items, attach, detach);
        await testCode();
        return await eventByItem.WaiyAsync(Timeout);
    }
}
