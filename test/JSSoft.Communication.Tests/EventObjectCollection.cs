// <copyright file="EventObjectCollection.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

namespace JSSoft.Communication.Tests;

public sealed class EventObjectCollection<TObject, TEventArgs>
    : Dictionary<TObject, ManualResetEvent>, IDisposable
    where TObject : notnull
    where TEventArgs : EventArgs
{
    private readonly Action<TObject, EventHandler<TEventArgs>> _detach;

    public EventObjectCollection(
        TObject[] items,
        Action<TObject, EventHandler<TEventArgs>> attach,
        Action<TObject, EventHandler<TEventArgs>> detach)
        : base(capacity: items.Length)
    {
        for (var i = 0; i < items.Length; i++)
        {
            Add(items[i], new ManualResetEvent(initialState: false));
            attach(items[i], Handler);
        }

        _detach = detach;
    }

    public void Dispose()
    {
        foreach (var item in this)
        {
            _detach(item.Key, Handler);
            item.Value.Dispose();
        }
    }

    public async Task<int> WaiyAsync(int timeout)
    {
        var tasks = Values.Select(item => Task.Run(() => item.WaitOne(timeout)));
        var results = await Task.WhenAll(tasks);
        return results.Count(item => item == true);
    }

    private void Handler(object? s, TEventArgs args)
    {
        if (s is TObject item && TryGetValue(item, out var manualResetEvent) == true)
        {
            manualResetEvent.Set();
        }
    }
}
