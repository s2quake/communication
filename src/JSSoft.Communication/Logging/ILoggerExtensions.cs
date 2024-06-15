// <copyright file="ILoggerExtensions.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;

namespace JSSoft.Communication.Logging;

public static class ILoggerExtensions
{
    public static void Debug(this ILogger logger, string format, params object[] args)
        => logger.Debug(string.Format(format, args));

    public static void Info(this ILogger logger, string format, params object[] args)
        => logger.Info(string.Format(format, args));

    public static void Error(this ILogger logger, string format, params object[] args)
        => logger.Error(string.Format(format, args));

    public static void Error(this ILogger logger, Exception e)
        => logger.Error(e);

    public static void Warn(this ILogger logger, string format, params object[] args)
        => logger.Warn(string.Format(format, args));

    public static void Fatal(this ILogger logger, string format, params object[] args)
        => logger.Fatal(string.Format(format, args));

    public static void Fatal(this ILogger logger, Exception e)
        => logger.Fatal(e);
}
