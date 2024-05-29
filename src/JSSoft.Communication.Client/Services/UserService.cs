// MIT License
// 
// Copyright (c) 2024 Jeesu Choi
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace JSSoft.Communication.Services;

[Export(typeof(IService))]
[Export(typeof(IUserService))]
[Export(typeof(INotifyUserService))]
sealed class UserService : ClientService<IUserService, IUserCallback>, IUserService, IUserCallback, INotifyUserService
{
    public Task CreateAsync(Guid token, string userID, string password, Authority authority, CancellationToken cancellationToken)
        => Server.CreateAsync(token, userID, password, authority, cancellationToken);

    public Task DeleteAsync(Guid token, string userID, CancellationToken cancellationToken)
        => Server.DeleteAsync(token, userID, cancellationToken);

    public Task RenameAsync(Guid token, string userName, CancellationToken cancellationToken)
        => Server.RenameAsync(token, userName, cancellationToken);

    public Task SetAuthorityAsync(Guid token, string userID, Authority authority, CancellationToken cancellationToken)
        => Server.SetAuthorityAsync(token, userID, authority, cancellationToken);

    public Task<Guid> LoginAsync(string userID, string password, CancellationToken cancellationToken)
        => Server.LoginAsync(userID, password, cancellationToken);

    public Task LogoutAsync(Guid token, CancellationToken cancellationToken)
        => Server.LogoutAsync(token, cancellationToken);

    public Task<(string userName, Authority authority)> GetInfoAsync(Guid token, string userID, CancellationToken cancellationToken)
        => Server.GetInfoAsync(token, userID, cancellationToken);

    public Task<string[]> GetUsersAsync(Guid token, CancellationToken cancellationToken)
        => Server.GetUsersAsync(token, cancellationToken);

    public Task<bool> IsOnlineAsync(Guid token, string userID, CancellationToken cancellationToken)
        => Server.IsOnlineAsync(token, userID, cancellationToken);

    public Task SendMessageAsync(Guid token, string userID, string message, CancellationToken cancellationToken)
        => Server.SendMessageAsync(token, userID, message, cancellationToken);

    public event EventHandler<UserEventArgs>? LoggedIn;

    public event EventHandler<UserEventArgs>? LoggedOut;

    public event EventHandler<UserEventArgs>? Created;

    public event EventHandler<UserEventArgs>? Deleted;

    public event EventHandler<UserMessageEventArgs>? MessageReceived;

    public event EventHandler<UserNameEventArgs>? Renamed;

    public event EventHandler<UserAuthorityEventArgs>? AuthorityChanged;

    #region IUserCallback

    void IUserCallback.OnCreated(string userID)
        => Created?.Invoke(this, new UserEventArgs(userID));

    void IUserCallback.OnDeleted(string userID)
        => Deleted?.Invoke(this, new UserEventArgs(userID));

    void IUserCallback.OnLoggedIn(string userID)
        => LoggedIn?.Invoke(this, new UserEventArgs(userID));

    void IUserCallback.OnLoggedOut(string userID)
        => LoggedOut?.Invoke(this, new UserEventArgs(userID));

    void IUserCallback.OnMessageReceived(string sender, string receiver, string message)
        => MessageReceived?.Invoke(this, new UserMessageEventArgs(sender, receiver, message));

    void IUserCallback.OnRenamed(string userID, string userName)
        => Renamed?.Invoke(this, new UserNameEventArgs(userID, userName));

    void IUserCallback.OnAuthorityChanged(string userID, Authority authority)
        => AuthorityChanged?.Invoke(this, new UserAuthorityEventArgs(userID, authority));

    #endregion
}
