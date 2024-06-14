// <copyright file="ConsoleLogger.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;

namespace JSSoft.Communication.Logging;

public sealed class ConsoleLogger : ILogger
{
    public static readonly ConsoleLogger Default = new();

    public void Debug(object message) => Console.WriteLine(message);

    public void Info(object message) => Console.WriteLine(message);

    public void Error(object message) => Console.Error.WriteLine(message);

    public void Warn(object message) => Console.WriteLine(message);

    public void Fatal(object message) => Console.Error.WriteLine(message);
}
