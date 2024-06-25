// <copyright file="DefaultSerializer.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;
using System.Text.Json;
using JSSoft.Communication.Converters;

namespace JSSoft.Communication;

internal sealed class DefaultSerializer : ISerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        IncludeFields = true,
        Converters =
        {
            new ArgumentExceptionConverter(),
            new ArgumentNullExceptionConverter(),
            new ArgumentOutOfRangeExceptionConverter(),
            new ExceptionConverter(),
            new IndexOutOfRangeExceptionConverter(),
            new InvalidOperationExceptionConverter(),
            new ObjectDisposedExceptionConverter(),
            new NotSupportedExceptionConverter(),
            new NullReferenceExceptionConverter(),
            new SystemExceptionConverter(),
            new ExceptionConverterFactory(),
        },
    };

    public string Serialize(Type type, object? data)
        => JsonSerializer.Serialize(data, type, Options);

    public object? Deserialize(Type type, string text)
        => JsonSerializer.Deserialize(text, type, Options);
}
