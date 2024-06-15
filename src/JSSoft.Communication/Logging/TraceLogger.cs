// <copyright file="TraceLogger.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System.Diagnostics;

namespace JSSoft.Communication.Logging;

public sealed class TraceLogger : ILogger
{
    public static readonly TraceLogger Default = new();

    public void Debug(object message) => Trace.TraceInformation($"{message}");

    public void Info(object message) => Trace.TraceInformation($"{message}");

    public void Error(object message) => Trace.TraceError($"{message}");

    public void Warn(object message) => Trace.TraceWarning($"{message}");

    public void Fatal(object message) => Trace.TraceError($"{message}");
}
