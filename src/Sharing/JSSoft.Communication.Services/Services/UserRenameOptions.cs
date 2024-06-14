// <copyright file="UserRenameOptions.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;

namespace JSSoft.Communication.Services;

public sealed record class UserRenameOptions
{
    public required Guid Token { get; init; }

    public required string UserName { get; init; }
}
