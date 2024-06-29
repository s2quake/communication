// <copyright file="ISerializerExtensions.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;
using System.Threading;

namespace JSSoft.Communication.Extensions;

internal static class ISerializerExtensions
{
    public static string[] SerializeMany(this ISerializer @this, Type[] types, object?[] args)
    {
        var items = new string[args.Length];
        for (var i = 0; i < args.Length; i++)
        {
            var type = types[i];
            var value = args[i];
            items[i] = @this.Serialize(type, value);
        }

        return items;
    }

    public static object?[] DeserializeMany(this ISerializer @this, Type[] types, string[] data)
    {
        var items = new object?[data.Length];
        for (var i = 0; i < data.Length; i++)
        {
            var type = types[i];
            var value = data[i];
            items[i] = @this.Deserialize(type, value);
        }

        return items;
    }

    public static object?[] DeserializeMany(
        this ISerializer @this, Type[] types, string[] data, CancellationToken? cancellationToken)
    {
        var length = cancellationToken != null ? data.Length + 1 : data.Length;
        var items = new object?[length];
        for (var i = 0; i < data.Length; i++)
        {
            var type = types[i];
            var value = data[i];
            items[i] = @this.Deserialize(type, value);
        }

        if (cancellationToken != null)
        {
            items[data.Length] = cancellationToken;
        }

        return items;
    }
}
