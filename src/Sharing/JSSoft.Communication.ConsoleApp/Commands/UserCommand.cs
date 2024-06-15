// <copyright file="UserCommand.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;
using System.ComponentModel.Composition;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JSSoft.Commands;
using JSSoft.Communication.ConsoleApp;
using JSSoft.Communication.Services;

namespace JSSoft.Communication.Commands;

[Export(typeof(ICommand))]
[method: ImportingConstructor]
internal class UserCommand(Application application, IUserService userService) : CommandMethodBase
{
    private readonly Application _application = application;
    private readonly IUserService _userService = userService;

    public override bool IsEnabled => _application.UserToken != Guid.Empty;

    [CommandMethod]
    public Task CreateAsync(
        string userId, string password, Authority authority, CancellationToken cancellationToken)
    {
        var options = new UserCreateOptions
        {
            Token = _application.UserToken,
            UserId = userId,
            Password = password,
            Authority = authority,
        };
        return _userService.CreateAsync(options, cancellationToken);
    }

    [CommandMethod]
    public Task DeleteAsync(string userId, CancellationToken cancellationToken)
    {
        var options = new UserDeleteOptions
        {
            Token = _application.UserToken,
            UserId = userId,
        };
        return _userService.DeleteAsync(options, cancellationToken);
    }

    [CommandMethod]
    public Task RenameAsync(string userName, CancellationToken cancellationToken)
    {
        var options = new UserRenameOptions
        {
            Token = _application.UserToken,
            UserName = userName,
        };
        return _userService.RenameAsync(options, cancellationToken);
    }

    [CommandMethod]
    public Task AuthorityAsync(
        string userId, Authority authority, CancellationToken cancellationToken)
    {
        var options = new UserAuthorityOptions
        {
            Token = _application.UserToken,
            UserId = userId,
            Authority = authority,
        };
        return _userService.SetAuthorityAsync(options, cancellationToken);
    }

    [CommandMethod]
    public async Task InfoAsync(string userId, CancellationToken cancellationToken)
    {
        var token = _application.UserToken;
        var userInfo = await _userService.GetInfoAsync(token, userId, cancellationToken);
        await Out.WriteLineAsync($"UseName: {userInfo.UserName}");
        await Out.WriteLineAsync($"Authority: {userInfo.Authority}");
    }

    [CommandMethod]
    public async Task ListAsync(CancellationToken cancellationToken)
    {
        var items = await _userService.GetUsersAsync(_application.UserToken, cancellationToken);
        var sb = new StringBuilder();
        foreach (var item in items)
        {
            sb.AppendLine(item);
        }

        await Out.WriteLineAsync(sb.ToString());
    }

    [CommandMethod]
    public Task SendMessageAsync(string userId, string message, CancellationToken cancellationToken)
    {
        var options = new UserSendMessageOptions
        {
            Token = _application.UserToken,
            UserId = userId,
            Message = message,
        };
        return _userService.SendMessageAsync(options, cancellationToken);
    }

    protected override bool IsMethodEnabled(CommandMethodDescriptor memberDescriptor)
    {
        return _application.UserToken != Guid.Empty;
    }
}
