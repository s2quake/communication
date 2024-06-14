// <copyright file="EmptyLogger.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

namespace JSSoft.Communication.Logging;

public class EmptyLogger : ILogger
{
    public static readonly EmptyLogger Default = new();

    public void Debug(object message)
    {
    }

    public void Info(object message)
    {
    }

    public void Error(object message)
    {
    }

    public void Warn(object message)
    {
    }

    public void Fatal(object message)
    {
    }
}
