// <copyright file="ServiceAttribute.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;

namespace JSSoft.Communication;

[AttributeUsage(AttributeTargets.Class)]
internal sealed class ServiceAttribute : Attribute
{
    public bool IsServer { get; set; }
}
