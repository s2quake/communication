// <copyright file="TestLogger.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using JSSoft.Communication.Logging;
using Xunit.Abstractions;

namespace JSSoft.Communication.Tests;

public sealed class TestLogger(ITestOutputHelper logger) : ILogger
{
    private readonly ITestOutputHelper _logger = logger;

    public void Debug(object message) => _logger.WriteLine($"{message}\n");

    public void Info(object message) => _logger.WriteLine($"{message}\n");

    public void Error(object message) => _logger.WriteLine($"{message}\n");

    public void Warn(object message) => _logger.WriteLine($"{message}\n");

    public void Fatal(object message) => _logger.WriteLine($"{message}\n");
}
