// <copyright file="LogUtility.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

namespace JSSoft.Communication.Logging;

public static class LogUtility
{
    public static ILogger Logger { get; set; } = TraceLogger.Default;

    public static LogLevel LogLevel { get; set; } = LogLevel.Fatal;

    private static ILogger LoggerInternal => Logger ?? EmptyLogger.Default;

    public static void Debug(object message)
    {
        if (LogLevel >= LogLevel.Debug)
        {
            LoggerInternal.Debug(message);
        }
    }

    public static void Info(object message)
    {
        if (LogLevel >= LogLevel.Info)
        {
            LoggerInternal.Info(message);
        }
    }

    public static void Error(object message)
    {
        if (LogLevel >= LogLevel.Error)
        {
            LoggerInternal.Error(message);
        }
    }

    public static void Warn(object message)
    {
        if (LogLevel >= LogLevel.Warn)
        {
            LoggerInternal.Warn(message);
        }
    }

    public static void Fatal(object message)
    {
        if (LogLevel >= LogLevel.Fatal)
        {
            LoggerInternal.Fatal(message);
        }
    }
}
