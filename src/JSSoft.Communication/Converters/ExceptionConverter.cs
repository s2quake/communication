// <copyright file="ExceptionConverter.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JSSoft.Communication.Converters;

internal sealed class ExceptionConverter : JsonConverter<Exception>
{
    public override Exception? Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.GetString() is string message ? new Exception(message) : null;

    public override void Write(
        Utf8JsonWriter writer, Exception value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Message);
}
