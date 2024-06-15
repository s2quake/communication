// <copyright file="UserEventArgs.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;

namespace JSSoft.Communication.Services;

public class UserEventArgs(string userId) : EventArgs
{
    public string UserId { get; } = userId;
}
