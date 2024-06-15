// <copyright file="IUserCallback.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

namespace JSSoft.Communication.Services;

public interface IUserCallback
{
    void OnCreated(string userId);

    void OnDeleted(string userId);

    void OnLoggedIn(string userId);

    void OnLoggedOut(string userId);

    void OnMessageReceived(string sender, string receiver, string message);

    void OnRenamed(string userId, string userName);

    void OnAuthorityChanged(string userId, Authority authority);
}
