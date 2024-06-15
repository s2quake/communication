// <copyright file="IUserService.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;

namespace JSSoft.Communication.Services;

[ServiceContract]
public interface IUserService
{
    Task CreateAsync(UserCreateOptions options, CancellationToken cancellationToken);

    Task DeleteAsync(UserDeleteOptions options, CancellationToken cancellationToken);

    Task RenameAsync(UserRenameOptions options, CancellationToken cancellationToken);

    Task SetAuthorityAsync(UserAuthorityOptions options, CancellationToken cancellationToken);

    Task<Guid> LoginAsync(UserLoginOptions options, CancellationToken cancellationToken);

    Task LogoutAsync(Guid token, CancellationToken cancellationToken);

    Task<UserInfo> GetInfoAsync(Guid token, string userId, CancellationToken cancellationToken);

    Task<string[]> GetUsersAsync(Guid token, CancellationToken cancellationToken);

    Task<bool> IsOnlineAsync(Guid token, string userId, CancellationToken cancellationToken);

    Task SendMessageAsync(UserSendMessageOptions options, CancellationToken cancellationToken);
}
