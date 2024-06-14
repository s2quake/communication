// <copyright file="InstanceMethodAttribute.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;

namespace JSSoft.Communication;

[AttributeUsage(AttributeTargets.Method)]
public sealed class InstanceMethodAttribute(string methodName) : Attribute
{
    public string MethodName { get; } = methodName;
}
