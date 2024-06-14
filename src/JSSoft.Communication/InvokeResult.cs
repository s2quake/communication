// <copyright file="InvokeResult.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;

namespace JSSoft.Communication;

public sealed record class InvokeResult
{
    public required string AssemblyQualifiedName { get; init; }

    public required Type ValueType { get; init; }

    public object? Value { get; init; }
}
