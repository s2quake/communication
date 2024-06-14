// <copyright file="UserCreateOptions.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;

namespace JSSoft.Communication.Services;

public sealed record class UserCreateOptions
{
    public required Guid Token { get; init; }

    public required string UserId { get; init; }

    public required string Password { get; init; }

    public Authority Authority { get; init; }
}
