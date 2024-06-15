// <copyright file="ISerializer.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;

namespace JSSoft.Communication;

public interface ISerializer
{
    string Serialize(Type type, object? data);

    object? Deserialize(Type type, string text);
}
