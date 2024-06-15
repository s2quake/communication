// <copyright file="UnreachableException.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

#if !NET7_0_OR_GREATER
using System;

namespace JSSoft.Communication;

class UnreachableException : SystemException
{
    public UnreachableException()
    {
    }

    public UnreachableException(string message)
        : base(message)
    {
    }
}
#endif // !NET6_0_OR_GREATER
