// <copyright file="UserAuthorityEventArgs.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

namespace JSSoft.Communication.Services;

public class UserAuthorityEventArgs(string userId, Authority authority) : UserEventArgs(userId)
{
    public Authority Authority { get; } = authority;
}
