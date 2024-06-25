// <copyright file="ObjectDisposedExceptionConverter.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JSSoft.Communication.Converters;

internal sealed class ObjectDisposedExceptionConverter : JsonConverter<ObjectDisposedException>
{
    public const string ObjectName = "objectName";
    public const string Message = "message";

    public override ObjectDisposedException? Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var props = reader.ReadObject(capacity: 2);
        var objectName = props[ObjectName] ?? throw new JsonException($"{ObjectName} is not found");
        var message = props[Message] ?? throw new JsonException($"{Message} is not found");
        return new ObjectDisposedException(objectName, message);
    }

    public override void Write(
        Utf8JsonWriter writer, ObjectDisposedException value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString(ObjectName, value.ObjectName);
        writer.WriteString(Message, value.Message);
        writer.WriteEndObject();
    }
}
