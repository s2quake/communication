// <copyright file="ArgumentExceptionTest.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using Xunit.Abstractions;

namespace JSSoft.Communication.Tests.Exceptions;

public sealed class ArgumentExceptionTest(ITestOutputHelper logger)
    : ExceptionTestBase<ArgumentException>(logger)
{
}
