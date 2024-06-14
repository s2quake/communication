// <copyright file="ExitCommand.cs" company="JSSoft">
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
internal sealed class ExitCommand(IApplication application) : CommandAsyncBase
{
    private readonly IApplication _application = application;

    [CommandPropertyRequired(DefaultValue = 0)]
    public int ExitCode { get; set; }

    protected override Task OnExecuteAsync(
        CancellationToken cancellationToken, IProgress<ProgressInfo> progress)
    {
        return _application.StopAsync(ExitCode);
    }
}
