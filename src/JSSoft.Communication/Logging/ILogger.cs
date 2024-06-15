// <copyright file="ILogger.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

namespace JSSoft.Communication.Logging;

public interface ILogger
{
    void Debug(object message);

    void Info(object message);

    void Error(object message);

    void Warn(object message);

    void Fatal(object message);
}
