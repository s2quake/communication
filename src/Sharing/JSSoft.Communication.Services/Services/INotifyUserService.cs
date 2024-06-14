// <copyright file="INotifyUserService.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;

namespace JSSoft.Communication.Services;

public interface INotifyUserService
{
    event EventHandler<UserEventArgs>? Created;

    event EventHandler<UserEventArgs>? Deleted;

    event EventHandler<UserEventArgs>? LoggedIn;

    event EventHandler<UserEventArgs>? LoggedOut;

    event EventHandler<UserMessageEventArgs>? MessageReceived;

    event EventHandler<UserNameEventArgs>? Renamed;

    event EventHandler<UserAuthorityEventArgs>? AuthorityChanged;
}
