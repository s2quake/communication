// <copyright file="UserService.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using JSSoft.Communication.Services;

namespace JSSoft.Communication.Client.Services;

[Export(typeof(IService))]
[Export(typeof(IUserService))]
[Export(typeof(INotifyUserService))]
internal sealed class UserService
    : ClientService<IUserService, IUserCallback>, IUserService, IUserCallback, INotifyUserService
{
    public event EventHandler<UserEventArgs>? LoggedIn;

    public event EventHandler<UserEventArgs>? LoggedOut;

    public event EventHandler<UserEventArgs>? Created;

    public event EventHandler<UserEventArgs>? Deleted;

    public event EventHandler<UserMessageEventArgs>? MessageReceived;

    public event EventHandler<UserNameEventArgs>? Renamed;

    public event EventHandler<UserAuthorityEventArgs>? AuthorityChanged;

    public Task CreateAsync(UserCreateOptions options, CancellationToken cancellationToken)
        => Server.CreateAsync(options, cancellationToken);

    public Task DeleteAsync(UserDeleteOptions options, CancellationToken cancellationToken)
        => Server.DeleteAsync(options, cancellationToken);

    public Task RenameAsync(UserRenameOptions options, CancellationToken cancellationToken)
        => Server.RenameAsync(options, cancellationToken);

    public Task SetAuthorityAsync(UserAuthorityOptions options, CancellationToken cancellationToken)
        => Server.SetAuthorityAsync(options, cancellationToken);

    public Task<Guid> LoginAsync(UserLoginOptions options, CancellationToken cancellationToken)
        => Server.LoginAsync(options, cancellationToken);

    public Task LogoutAsync(Guid token, CancellationToken cancellationToken)
        => Server.LogoutAsync(token, cancellationToken);

    public Task<UserInfo> GetInfoAsync(
        Guid token, string userId, CancellationToken cancellationToken)
        => Server.GetInfoAsync(token, userId, cancellationToken);

    public Task<string[]> GetUsersAsync(Guid token, CancellationToken cancellationToken)
        => Server.GetUsersAsync(token, cancellationToken);

    public Task<bool> IsOnlineAsync(Guid token, string userId, CancellationToken cancellationToken)
        => Server.IsOnlineAsync(token, userId, cancellationToken);

    public Task SendMessageAsync(
        UserSendMessageOptions options, CancellationToken cancellationToken)
        => Server.SendMessageAsync(options, cancellationToken);

    void IUserCallback.OnCreated(string userId)
        => Created?.Invoke(this, new UserEventArgs(userId));

    void IUserCallback.OnDeleted(string userId)
        => Deleted?.Invoke(this, new UserEventArgs(userId));

    void IUserCallback.OnLoggedIn(string userId)
        => LoggedIn?.Invoke(this, new UserEventArgs(userId));

    void IUserCallback.OnLoggedOut(string userId)
        => LoggedOut?.Invoke(this, new UserEventArgs(userId));

    void IUserCallback.OnMessageReceived(string sender, string receiver, string message)
        => MessageReceived?.Invoke(this, new UserMessageEventArgs(sender, receiver, message));

    void IUserCallback.OnRenamed(string userId, string userName)
        => Renamed?.Invoke(this, new UserNameEventArgs(userId, userName));

    void IUserCallback.OnAuthorityChanged(string userId, Authority authority)
        => AuthorityChanged?.Invoke(this, new UserAuthorityEventArgs(userId, authority));
}
