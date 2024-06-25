// <copyright file="ExceptionConverterFactory.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JSSoft.Communication.Converters;

internal sealed class ExceptionConverterFactory : JsonConverterFactory
{
    private readonly Dictionary<Type, JsonConverter> _converterByType = [];

    public override bool CanConvert(Type typeToConvert)
        => typeof(Exception).IsAssignableFrom(typeToConvert);

    public override JsonConverter? CreateConverter(
        Type typeToConvert, JsonSerializerOptions options)
    {
        if (_converterByType.TryGetValue(typeToConvert, out var converter) != true)
        {
            var genericType = typeof(InternalJsonConverter<>).MakeGenericType(typeToConvert);
            converter = (JsonConverter)Activator.CreateInstance(genericType)!;
            _converterByType.Add(typeToConvert, converter);
        }

        return converter;
    }

    private sealed class InternalJsonConverter<T> : JsonConverter<T>
        where T : Exception
    {
        public override T? Read(
            ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.GetString() is string message
                ? (T)Activator.CreateInstance(typeToConvert, message)!
                : null;

        public override void Write(
            Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.Message);
    }
}
