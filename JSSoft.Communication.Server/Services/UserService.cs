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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JSSoft.Communication.Threading;

namespace JSSoft.Communication.Services;

[Export(typeof(IService))]
[Export(typeof(IUserService))]
[Export(typeof(INotifyUserService))]
sealed class UserService : ServerService<IUserService, IUserCallback>, IUserService, INotifyUserService, IDisposable
{
    private readonly Dictionary<string, UserInfo> _userByID = [];
    private readonly Dictionary<Guid, UserInfo> _userByToken = [];

    private Dispatcher? _dispatcher;

    public UserService()
    {
        _userByID.Add("admin", new UserInfo()
        {
            UserID = "admin",
            Password = "admin",
            UserName = "Administrator",
            Authority = Authority.Admin,
        });

        for (var i = 0; i < 10; i++)
        {
            var user = new UserInfo()
            {
                UserID = $"user{i}",
                Password = "1234",
                UserName = $"사용자{i}",
                Authority = Authority.Member,
            };
            _userByID.Add(user.UserID, user);
        }
        _dispatcher = new Dispatcher(this);
    }

    public Task CreateAsync(Guid token, string userID, string password, Authority authority, CancellationToken cancellationToken)
    {
        return Dispatcher.InvokeAsync(() =>
        {
            ValidateCreate(token, userID, password);

            var user = _userByToken[token];
            var userInfo = new UserInfo()
            {
                UserID = userID,
                Password = password,
                UserName = string.Empty,
                Authority = authority
            };
            _userByID.Add(userID, userInfo);
            Client.OnCreated(userID);
            Created?.Invoke(this, new UserEventArgs(userID));
        });
    }

    public Task DeleteAsync(Guid token, string userID, CancellationToken cancellationToken)
    {
        return Dispatcher.InvokeAsync(() =>
        {
            ValidateDelete(token, userID);

            _userByID.Remove(userID);
            Client.OnDeleted(userID);
            Deleted?.Invoke(this, new UserEventArgs(userID));
        });
    }

    public Task<(string, Authority)> GetInfoAsync(Guid token, string userID, CancellationToken cancellationToken)
    {
        return Dispatcher.InvokeAsync(() =>
        {
            ValidateUser(token);
            ValidateUser(userID);

            var user = _userByID[userID];
            return (user.UserName, user.Authority);
        });
    }

    public Task<string[]> GetUsersAsync(Guid token, CancellationToken cancellationToken)
    {
        return Dispatcher.InvokeAsync(() =>
        {
            ValidateUser(token);

            return _userByID.Keys.ToArray();
        });
    }

    public Task<bool> IsOnlineAsync(Guid token, string userID, CancellationToken cancellationToken)
    {
        return Dispatcher.InvokeAsync(() =>
        {
            ValidateUser(token);
            ValidateUser(userID);

            var user = _userByID[userID];
            return user.Token != Guid.Empty;
        });
    }

    public Task<Guid> LoginAsync(string userID, string password, CancellationToken cancellationToken)
    {
        return Dispatcher.InvokeAsync(() =>
        {
            ValidatePassword(userID, password);

            var token = Guid.NewGuid();
            var user = _userByID[userID];
            user.Token = token;
            _userByToken.Add(token, user);
            Client.OnLoggedIn(userID);
            LoggedIn?.Invoke(this, new UserEventArgs(userID));
            return token;
        });
    }

    public Task LogoutAsync(Guid token, CancellationToken cancellationToken)
    {
        return Dispatcher.InvokeAsync(() =>
        {
            ValidateUser(token);

            var user = _userByToken[token];
            user.Token = Guid.Empty;
            _userByToken.Remove(token);
            Client.OnLoggedOut(user.UserID);
            LoggedOut?.Invoke(this, new UserEventArgs(user.UserID));
        });
    }

    public Task SendMessageAsync(Guid token, string userID, string message, CancellationToken cancellationToken)
    {
        return Dispatcher.InvokeAsync(() =>
        {
            ValidateUser(token);
            ValidateOnline(userID);
            ValidateMessage(message);

            var user = _userByToken[token];
            Client.OnMessageReceived(user.UserID, userID, message);
            MessageReceived?.Invoke(this, new UserMessageEventArgs(user.UserID, userID, message));
        });
    }

    public Task RenameAsync(Guid token, string userName, CancellationToken cancellationToken)
    {
        return Dispatcher.InvokeAsync(() =>
        {
            ValidateRename(token, userName);

            var user = _userByToken[token];
            user.UserName = userName;
            Client.OnRenamed(user.UserID, userName);
            Renamed?.Invoke(this, new UserNameEventArgs(user.UserID, userName));
        });
    }

    public Task SetAuthorityAsync(Guid token, string userID, Authority authority, CancellationToken cancellationToken)
    {
        return Dispatcher.InvokeAsync(() =>
        {
            ValidateSetAuthority(token, userID);

            var user = _userByID[userID];
            user.Authority = authority;
            Client.OnAuthorityChanged(userID, authority);
            AuthorityChanged?.Invoke(this, new UserAuthorityEventArgs(userID, authority));
        });
    }

    public void Dispose()
    {
        if (_dispatcher == null)
            throw new ObjectDisposedException($"{this}");

        _dispatcher.Dispose();
        _dispatcher = null;
        GC.SuppressFinalize(this);
    }

    public Dispatcher Dispatcher
    {
        get
        {
            if (_dispatcher == null)
                throw new InvalidOperationException($"'{this}' has not been initialized.");
            return _dispatcher;
        }
    }

    public event EventHandler<UserEventArgs>? Created;

    public event EventHandler<UserEventArgs>? Deleted;

    public event EventHandler<UserEventArgs>? LoggedIn;

    public event EventHandler<UserEventArgs>? LoggedOut;

    public event EventHandler<UserMessageEventArgs>? MessageReceived;

    public event EventHandler<UserNameEventArgs>? Renamed;

    public event EventHandler<UserAuthorityEventArgs>? AuthorityChanged;

    private void ValidateUser(string userID)
    {
        Dispatcher.VerifyAccess();
        if (userID == null)
            throw new ArgumentNullException(nameof(userID));
        if (_userByID.ContainsKey(userID) != true)
            throw new ArgumentException("Invalid userID", nameof(userID));
    }

    private void ValidateNotUser(string userID)
    {
        Dispatcher.VerifyAccess();
        if (userID == null)
            throw new ArgumentNullException(nameof(userID));
        if (_userByID.ContainsKey(userID) == true)
            throw new ArgumentException("user is already exists.", nameof(userID));
    }

    private void ValidateUser(Guid token)
    {
        Dispatcher.VerifyAccess();
        if (_userByToken.ContainsKey(token) != true)
            throw new ArgumentException("Invalid token.", nameof(token));
    }

    private void ValidatePassword(string password)
    {
        Dispatcher.VerifyAccess();
        if (password == null)
            throw new ArgumentNullException(nameof(password));
        if (password == string.Empty)
            throw new ArgumentException("Invalid password.", nameof(password));
        if (password.Length < 4)
            throw new ArgumentException("length of password must be greater or equal than 4.", nameof(password));
    }

    private void ValidatePassword(string userID, string password)
    {
        Dispatcher.VerifyAccess();
        ValidateUser(userID);
        if (password == null)
            throw new ArgumentNullException(nameof(password));
        var user = _userByID[userID];
        if (user.Password != password)
            throw new InvalidOperationException("wrong userID or password.");
    }

    private void ValidateCreate(Guid token, string userID, string password)
    {
        Dispatcher.VerifyAccess();
        ValidateUser(token);
        ValidateNotUser(userID);
        ValidatePassword(password);
        var user = _userByToken[token];
        if (user.Authority != Authority.Admin)
            throw new InvalidOperationException("permission denied.");
    }

    private void ValidateMessage(string message)
    {
        Dispatcher.VerifyAccess();
        if (message == null)
            throw new ArgumentNullException(nameof(message));
    }

    private void ValidateOnline(string userID)
    {
        Dispatcher.VerifyAccess();
        ValidateUser(userID);
        var user = _userByID[userID];

        if (user.Token == Guid.Empty)
            throw new InvalidOperationException($"user is offline.");
    }

    private void ValidateRename(Guid token, string userName)
    {
        Dispatcher.VerifyAccess();
        ValidateUser(token);
        if (userName == null)
            throw new ArgumentNullException(nameof(userName));
        if (userName == string.Empty)
            throw new ArgumentException("Invalid name.", nameof(userName));
        var user = _userByToken[token];
        if (user.UserName == userName)
            throw new ArgumentException("same name can not set.", nameof(userName));
        if (user.UserID == "admin")
            throw new InvalidOperationException("permission denied.");
    }

    private void ValidateSetAuthority(Guid token, string userID)
    {
        Dispatcher.VerifyAccess();
        ValidateUser(token);
        ValidateUser(userID);
        var user1 = _userByToken[token];
        if (user1.UserID == userID)
            throw new InvalidOperationException("can not set authority.");
        if (user1.Authority != Authority.Admin)
            throw new InvalidOperationException("permission denied.");
        var user2 = _userByID[userID];
        if (user2.Token != Guid.Empty)
            throw new InvalidOperationException("can not set authority of online user.");
        if (userID == "admin")
            throw new InvalidOperationException("permission denied.");
    }

    private void ValidateDelete(Guid token, string userID)
    {
        Dispatcher.VerifyAccess();
        ValidateUser(token);
        ValidateNotUser(userID);
        var user1 = _userByToken[token];
        if (user1.Authority != Authority.Admin)
            throw new InvalidOperationException("permission denied.");
        var user2 = _userByID[userID];
        if (user2.Token != Guid.Empty)
            throw new InvalidOperationException("can not delete online user.");
        if (userID == "admin")
            throw new InvalidOperationException("permission denied.");
    }
}
