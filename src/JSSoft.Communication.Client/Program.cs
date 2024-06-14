// <copyright file="Program.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;
using JSSoft.Communication.ConsoleApp;

try
{
    var options = ApplicationOptions.Parse(args);
    using var application = new Application(options);
    await application.StartAsync();
}
catch (Exception e)
{
    Console.Error.WriteLine(e);
    Console.ReadKey();
    Environment.Exit(1);
}
