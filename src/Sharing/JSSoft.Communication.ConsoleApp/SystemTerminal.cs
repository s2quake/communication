// <copyright file="SystemTerminal.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System.ComponentModel.Composition;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using JSSoft.Commands.Extensions;
using JSSoft.Terminals;

namespace JSSoft.Communication.ConsoleApp;

[Export]
[method: ImportingConstructor]
internal sealed class SystemTerminal(Application application, CommandContext commandContext)
    : SystemTerminalBase
{
    private static readonly string Postfix = TerminalEnvironment.IsWindows() == true ? "> " : "$ ";
    private readonly Application _application = application;
    private readonly CommandContext _commandContext = commandContext;

    protected override Task OnExecuteAsync(string command, CancellationToken cancellationToken)
    {
        return _commandContext.ExecuteAsync(command, cancellationToken);
    }

    protected override void OnInitialize(TextWriter @out, TextWriter error)
    {
        _commandContext.Out = @out;
        _commandContext.Error = error;
    }

    protected override string FormatPrompt(string prompt)
    {
        if (_application.UserId == string.Empty)
        {
            return prompt;
        }
        else
        {
            var tb = new TerminalStringBuilder();
            var pattern = $"(.+@)(.+).{{{Postfix.Length}}}";
            var match = Regex.Match(prompt, pattern);
            tb.Append(match.Groups[1].Value);
            tb.Foreground = TerminalColorType.BrightGreen;
            tb.Append(match.Groups[2].Value);
            tb.Foreground = null;
            tb.Append(Postfix);
            return tb.ToString();
        }
    }
}
