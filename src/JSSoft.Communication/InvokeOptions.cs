// <copyright file="InvokeOptions.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;

namespace JSSoft.Communication;

public sealed record class InvokeOptions
{
    public required InstanceBase Instance { get; init; }

    public string Name { get; init; } = string.Empty;

    public Type[] Types { get; init; } = [];

    public object?[] Args { get; init; } = [];
}
