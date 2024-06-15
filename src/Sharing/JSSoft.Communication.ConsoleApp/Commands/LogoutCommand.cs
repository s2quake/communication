// <copyright file="LogoutCommand.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using JSSoft.Commands;
using JSSoft.Communication.ConsoleApp;
using JSSoft.Communication.Services;

namespace JSSoft.Communication.Commands;

[Export(typeof(ICommand))]
[method: ImportingConstructor]
internal sealed class LogoutCommand(Application application, IUserService userService)
    : CommandAsyncBase
{
    private readonly Application _application = application;
    private readonly IUserService _userService = userService;

    public override bool IsEnabled => _application.UserToken != Guid.Empty;

    protected override async Task OnExecuteAsync(
        CancellationToken cancellationToken, IProgress<ProgressInfo> progress)
    {
        await _userService.LogoutAsync(_application.UserToken, cancellationToken);
        _application.Logout();
    }
}
