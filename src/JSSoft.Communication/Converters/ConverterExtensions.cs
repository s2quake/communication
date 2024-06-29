// <copyright file="ConverterExtensions.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System.Collections.Generic;
using System.Text.Json;

namespace JSSoft.Communication.Converters;

internal static class ConverterExtensions
{
    public static IDictionary<string, string?> ReadObject(
        this ref Utf8JsonReader @this, int capacity)
    {
        var props = new Dictionary<string, string?>(capacity);
        if (@this.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Invalid Json format");
        }

        while (@this.Read())
        {
            if (@this.TokenType == JsonTokenType.EndObject)
            {
                @this.Read();
                return props;
            }

            if (@this.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Invalid Json format");
            }

            var key = @this.GetString() ?? throw new JsonException("Invalid Json format");
            @this.Read();
            var value = @this.GetString();
            props.Add(key, value);
        }

        throw new JsonException("Invalid Json format");
    }
}
