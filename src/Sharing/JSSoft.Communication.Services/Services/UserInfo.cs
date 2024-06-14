// <copyright file="UserInfo.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;

namespace JSSoft.Communication.Services;

public readonly record struct UserInfo
{
    public string UserName { get; init; }

    public Authority Authority { get; init; }
}
