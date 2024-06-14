// <copyright file="JsonSerializer.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;
using Newtonsoft.Json;

namespace JSSoft.Communication;

internal sealed class JsonSerializer : ISerializer
{
    private static readonly JsonSerializerSettings Settings = new();

    public string Serialize(Type type, object? data)
        => JsonConvert.SerializeObject(data, type, Settings);

    public object? Deserialize(Type type, string text)
        => JsonConvert.DeserializeObject(text, type, Settings);
}
