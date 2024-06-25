// <copyright file="ArgumentOutOfRangeExceptionConverter.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JSSoft.Communication.Converters;

internal sealed class ArgumentOutOfRangeExceptionConverter
    : JsonConverter<ArgumentOutOfRangeException>
{
    public const string ParamName = "paramName";
    public const string Message = "message";

    public override ArgumentOutOfRangeException? Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var props = reader.ReadObject(capacity: 2);
        var message = props[Message] ?? throw new JsonException($"{Message} is not found");
        return new ArgumentOutOfRangeException(message);
    }

    public override void Write(
        Utf8JsonWriter writer, ArgumentOutOfRangeException value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString(ParamName, value.ParamName);
        writer.WriteString(Message, value.Message);
        writer.WriteEndObject();
    }
}
