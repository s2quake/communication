// <copyright file="ServiceUtility.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;
using System.Reflection;

namespace JSSoft.Communication;

public static class ServiceUtility
{
    public static Type ValidateServerType(Type serviceType)
    {
        if (serviceType.IsInterface != true)
        {
            throw new InvalidOperationException("Server type must be interface.");
        }

        if (IsNestedPublicType(serviceType) != true
            && IsPublicType(serviceType) != true
            && IsInternalType(serviceType) != true)
        {
            throw new InvalidOperationException(
                $"'{serviceType.Name}' must be public or internal.");
        }

        return serviceType;
    }

    public static Type ValidateClientType(Type callbackType)
    {
        if (callbackType != typeof(void))
        {
            if (callbackType.IsInterface != true)
            {
                throw new InvalidOperationException("Client type must be interface.");
            }

            if (IsNestedPublicType(callbackType) != true
                && IsPublicType(callbackType) != true
                && IsInternalType(callbackType) != true)
            {
                throw new InvalidOperationException(
                    $"'{callbackType.Name}' must be public or internal.");
            }
        }

        return callbackType;
    }

    public static bool IsNestedPublicType(Type type)
        => type.IsNested == true && type.IsNestedPublic == true;

    public static bool IsPublicType(Type type)
        => type.IsVisible == true && type.IsPublic == true && type.IsNotPublic != true;

    public static bool IsInternalType(Type type)
        => type.IsVisible != true && type.IsPublic != true && type.IsNotPublic == true;

    public static bool IsServer(IService service)
    {
        var attribute = service.GetType().GetCustomAttribute(typeof(ServiceAttribute));
        if (attribute is ServiceAttribute serviceAttribute)
        {
            return serviceAttribute.IsServer;
        }

        return false;
    }
}
