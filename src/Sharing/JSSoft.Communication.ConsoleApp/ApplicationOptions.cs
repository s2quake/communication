// <copyright file="ApplicationOptions.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System.ComponentModel.Composition;
using JSSoft.Commands;

namespace JSSoft.Communication.ConsoleApp;

[Export]
public class ApplicationOptions
{
    [CommandPropertyRequired(DefaultValue = ServiceContextBase.DefaultHost)]
    public string Host { get; set; } = string.Empty;

    [CommandProperty(InitValue = ServiceContextBase.DefaultPort)]
    public int Port { get; set; }

    [CommandPropertySwitch]
    public bool Verbose { get; set; }

    public static ApplicationOptions Parse(string[] args)
    {
        var options = new ApplicationOptions();
        var parserSettings = new CommandSettings()
        {
            AllowEmpty = true,
        };
        var parser = new CommandParser(options, parserSettings);
        parser.Parse(args);
        return options;
    }
}
