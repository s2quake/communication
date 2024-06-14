// <copyright file="UserMessageEventArgs.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;

namespace JSSoft.Communication.Services;

public class UserMessageEventArgs(string sender, string receiver, string message) : EventArgs
{
    public string Sender { get; } = sender;

    public string Receiver { get; } = receiver;

    public string Message { get; } = message;
}
