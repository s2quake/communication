// <copyright file="UserService.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JSSoft.Communication.Services;
using JSSoft.Communication.Threading;

namespace JSSoft.Communication.Server.Services;

[Export(typeof(IService))]
[Export(typeof(IUserService))]
[Export(typeof(INotifyUserService))]
internal sealed class UserService
    : ServerService<IUserService, IUserCallback>, IUserService, INotifyUserService, IDisposable
{
    private readonly Dictionary<string, User> _userById = [];
    private readonly Dictionary<Guid, User> _userByToken = [];

    private Dispatcher? _dispatcher;

    public UserService()
    {
        _userById.Add("admin", new User()
        {
            UserId = "admin",
            Password = "admin",
            UserName = "Administrator",
            Authority = Authority.Admin,
        });

        for (var i = 0; i < 10; i++)
        {
            var user = new User()
            {
                UserId = $"user{i}",
                Password = "1234",
                UserName = $"사용자{i}",
                Authority = Authority.Member,
            };
            _userById.Add(user.UserId, user);
        }

        _dispatcher = new Dispatcher(this);
    }

    public event EventHandler<UserEventArgs>? Created;

    public event EventHandler<UserEventArgs>? Deleted;

    public event EventHandler<UserEventArgs>? LoggedIn;

    public event EventHandler<UserEventArgs>? LoggedOut;

    public event EventHandler<UserMessageEventArgs>? MessageReceived;

    public event EventHandler<UserNameEventArgs>? Renamed;

    public event EventHandler<UserAuthorityEventArgs>? AuthorityChanged;

    public Dispatcher Dispatcher => _dispatcher
        ?? throw new InvalidOperationException($"'{this}' has not been initialized.");

    public Task CreateAsync(UserCreateOptions options, CancellationToken cancellationToken)
    {
        return Dispatcher.InvokeAsync(() =>
        {
            ValidateCreate(options);

            var user = new User()
            {
                UserId = options.UserId,
                Password = options.Password,
                UserName = string.Empty,
                Authority = options.Authority,
            };
            _userById.Add(options.UserId, user);
            Client.OnCreated(options.UserId);
            Created?.Invoke(this, new UserEventArgs(options.UserId));
        });
    }

    public Task DeleteAsync(UserDeleteOptions options, CancellationToken cancellationToken)
    {
        return Dispatcher.InvokeAsync(() =>
        {
            ValidateDelete(options);

            var userId = options.UserId;
            _userById.Remove(userId);
            Client.OnDeleted(userId);
            Deleted?.Invoke(this, new UserEventArgs(userId));
        });
    }

    public Task<UserInfo> GetInfoAsync(
        Guid token, string userId, CancellationToken cancellationToken)
    {
        return Dispatcher.InvokeAsync(() =>
        {
            ValidateUser(token);
            ValidateUser(userId);

            var user = _userById[userId];
            return new UserInfo
            {
                UserName = user.UserName,
                Authority = user.Authority,
            };
        });
    }

    public Task<string[]> GetUsersAsync(Guid token, CancellationToken cancellationToken)
    {
        return Dispatcher.InvokeAsync(() =>
        {
            ValidateUser(token);

            return _userById.Keys.ToArray();
        });
    }

    public Task<bool> IsOnlineAsync(Guid token, string userId, CancellationToken cancellationToken)
    {
        return Dispatcher.InvokeAsync(() =>
        {
            ValidateUser(token);
            ValidateUser(userId);

            var user = _userById[userId];
            return user.Token != Guid.Empty;
        });
    }

    public Task<Guid> LoginAsync(UserLoginOptions options, CancellationToken cancellationToken)
    {
        return Dispatcher.InvokeAsync(() =>
        {
            var userId = options.UserId;
            var password = options.Password;
            ValidatePassword(userId, password);

            var token = Guid.NewGuid();
            var user = _userById[userId];
            user.Token = token;
            _userByToken.Add(token, user);
            Client.OnLoggedIn(userId);
            LoggedIn?.Invoke(this, new UserEventArgs(userId));
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
            Client.OnLoggedOut(user.UserId);
            LoggedOut?.Invoke(this, new UserEventArgs(user.UserId));
        });
    }

    public Task SendMessageAsync(
        UserSendMessageOptions options, CancellationToken cancellationToken)
    {
        return Dispatcher.InvokeAsync(() =>
        {
            var token = options.Token;
            var userId = options.UserId;
            var message = options.Message;
            ValidateUser(token);
            ValidateOnline(userId);
            ValidateMessage(message);

            var user = _userByToken[token];
            Client.OnMessageReceived(user.UserId, userId, message);
            MessageReceived?.Invoke(this, new UserMessageEventArgs(user.UserId, userId, message));
        });
    }

    public Task RenameAsync(UserRenameOptions options, CancellationToken cancellationToken)
    {
        return Dispatcher.InvokeAsync(() =>
        {
            var token = options.Token;
            var userName = options.UserName;
            ValidateRename(token, userName);

            var user = _userByToken[token];
            user.UserName = userName;
            Client.OnRenamed(user.UserId, userName);
            Renamed?.Invoke(this, new UserNameEventArgs(user.UserId, userName));
        });
    }

    public Task SetAuthorityAsync(UserAuthorityOptions options, CancellationToken cancellationToken)
    {
        return Dispatcher.InvokeAsync(() =>
        {
            var token = options.Token;
            var userId = options.UserId;
            var authority = options.Authority;
            ValidateSetAuthority(token, userId);

            var user = _userById[userId];
            user.Authority = authority;
            Client.OnAuthorityChanged(userId, authority);
            AuthorityChanged?.Invoke(this, new UserAuthorityEventArgs(userId, authority));
        });
    }

    public void Dispose()
    {
        ObjectDisposedException.ThrowIf(_dispatcher == null, $"{this}");

        _dispatcher.Dispose();
        _dispatcher = null;
        GC.SuppressFinalize(this);
    }

    private void ValidateUser(string userId)
    {
        Dispatcher.VerifyAccess();
        if (userId == null)
        {
            throw new ArgumentNullException(nameof(userId));
        }

        if (_userById.ContainsKey(userId) != true)
        {
            throw new ArgumentException("Invalid userID", nameof(userId));
        }
    }

    private void ValidateUser(Guid token)
    {
        Dispatcher.VerifyAccess();
        if (_userByToken.ContainsKey(token) != true)
        {
            throw new ArgumentException("Invalid token.", nameof(token));
        }
    }

    private void ValidateNotUser(string userId)
    {
        Dispatcher.VerifyAccess();
        if (userId == null)
        {
            throw new ArgumentNullException(nameof(userId));
        }

        if (_userById.ContainsKey(userId) == true)
        {
            throw new ArgumentException("user is already exists.", nameof(userId));
        }
    }

    private void ValidatePassword(string password)
    {
        Dispatcher.VerifyAccess();
        if (password == null)
        {
            throw new ArgumentNullException(nameof(password));
        }

        if (password == string.Empty)
        {
            throw new ArgumentException("Invalid password.", nameof(password));
        }

        if (password.Length < 4)
        {
            throw new ArgumentException(
                "length of password must be greater or equal than 4.", nameof(password));
        }
    }

    private void ValidatePassword(string userId, string password)
    {
        Dispatcher.VerifyAccess();
        ValidateUser(userId);
        if (password == null)
        {
            throw new ArgumentNullException(nameof(password));
        }

        var user = _userById[userId];
        if (user.Password != password)
        {
            throw new InvalidOperationException("wrong userID or password.");
        }
    }

    private void ValidateCreate(UserCreateOptions options)
    {
        Dispatcher.VerifyAccess();
        ValidateUser(options.Token);
        ValidateNotUser(options.UserId);
        ValidatePassword(options.Password);
        var user = _userByToken[options.Token];
        if (user.Authority != Authority.Admin)
        {
            throw new InvalidOperationException("permission denied.");
        }
    }

    private void ValidateMessage(string message)
    {
        Dispatcher.VerifyAccess();
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }
    }

    private void ValidateOnline(string userId)
    {
        Dispatcher.VerifyAccess();
        ValidateUser(userId);
        var user = _userById[userId];

        if (user.Token == Guid.Empty)
        {
            throw new InvalidOperationException($"user is offline.");
        }
    }

    private void ValidateRename(Guid token, string userName)
    {
        Dispatcher.VerifyAccess();
        ValidateUser(token);
        if (userName == null)
        {
            throw new ArgumentNullException(nameof(userName));
        }

        if (userName == string.Empty)
        {
            throw new ArgumentException("Invalid name.", nameof(userName));
        }

        var user = _userByToken[token];
        if (user.UserName == userName)
        {
            throw new ArgumentException("same name can not set.", nameof(userName));
        }

        if (user.UserId == "admin")
        {
            throw new InvalidOperationException("permission denied.");
        }
    }

    private void ValidateSetAuthority(Guid token, string userId)
    {
        Dispatcher.VerifyAccess();
        ValidateUser(token);
        ValidateUser(userId);
        var user1 = _userByToken[token];
        if (user1.UserId == userId)
        {
            throw new InvalidOperationException("can not set authority.");
        }

        if (user1.Authority != Authority.Admin)
        {
            throw new InvalidOperationException("permission denied.");
        }

        var user2 = _userById[userId];
        if (user2.Token != Guid.Empty)
        {
            throw new InvalidOperationException("can not set authority of online user.");
        }

        if (userId == "admin")
        {
            throw new InvalidOperationException("permission denied.");
        }
    }

    private void ValidateDelete(UserDeleteOptions options)
    {
        Dispatcher.VerifyAccess();
        ValidateUser(options.Token);
        ValidateNotUser(options.UserId);
        var user1 = _userByToken[options.Token];
        if (user1.Authority != Authority.Admin)
        {
            throw new InvalidOperationException("permission denied.");
        }

        var user2 = _userById[options.UserId];
        if (user2.Token != Guid.Empty)
        {
            throw new InvalidOperationException("can not delete online user.");
        }

        if (options.UserId == "admin")
        {
            throw new InvalidOperationException("permission denied.");
        }
    }
}
