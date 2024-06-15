// <copyright file="CommandContext.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System.Collections.Generic;
using System.ComponentModel.Composition;
using JSSoft.Commands;
using JSSoft.Communication.Commands;
using JSSoft.Terminals;

namespace JSSoft.Communication.ConsoleApp;

[Export(typeof(CommandContext))]
[method: ImportingConstructor]
internal sealed class CommandContext(
    [ImportMany] IEnumerable<ICommand> commands,
    HelpCommand helpCommand,
    VersionCommand versionCommand)
    : CommandContextBase(commands)
{
    protected override ICommand HelpCommand { get; } = helpCommand;

    protected override ICommand VersionCommand { get; } = versionCommand;

    protected override void OnEmptyExecute()
    {
        var tsb = new TerminalStringBuilder
        {
            Foreground = TerminalColorType.BrightGreen,
        };
        tsb.AppendLine("Type '--help | -h' for usage.");
        tsb.AppendLine("Type 'exit' to exit application.");
        tsb.ResetOptions();
        tsb.AppendEnd();
        Out.Write(tsb.ToString());
    }
}
