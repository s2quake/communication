// <copyright file="ObjectDisposedException.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

#if !NET7_0_OR_GREATER
#pragma warning disable
namespace JSSoft.Communication;

internal class ObjectDisposedException(string objectName)
    : System.ObjectDisposedException(objectName)
{
    public static void ThrowIf(bool condition, object instance)
    {
        if (condition == true)
        {
            throw new System.ObjectDisposedException(instance.GetType().Name);
        }
    }
}

#endif // !NET7_0_OR_GREATER
