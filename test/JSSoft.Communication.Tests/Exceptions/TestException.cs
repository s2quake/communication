// <copyright file="TestException.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

namespace JSSoft.Communication.Tests.Exceptions;

public sealed class TestException : Exception
{
    public TestException()
    {
    }

    public TestException(string message)
        : base(message)
    {
    }
}
