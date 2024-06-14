// <copyright file="OpenCommand.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using JSSoft.Commands;
using JSSoft.Communication.ConsoleApp;

namespace JSSoft.Communication.Commands;

[Export(typeof(ICommand))]
[method: ImportingConstructor]
internal sealed class OpenCommand(IServiceContext serviceContext, Application application)
    : CommandAsyncBase
{
    private readonly IServiceContext _serviceContext = serviceContext;
    private readonly Application _application = application;

    public override bool IsEnabled => _serviceContext.ServiceState == ServiceState.None;

    protected override async Task OnExecuteAsync(
        CancellationToken cancellationToken, IProgress<ProgressInfo> progress)
    {
        try
        {
            _application.Token = await _serviceContext.OpenAsync(cancellationToken);
        }
        catch
        {
            await _serviceContext.AbortAsync();
            throw;
        }
    }
}
