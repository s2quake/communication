// <copyright file="MethodUtility.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace JSSoft.Communication;

public static class MethodUtility
{
    public static string GenerateName(MethodInfo methodInfo)
    {
        var returnType = methodInfo.ReturnType;
        var reflectedType = methodInfo.ReflectedType
            ?? throw new ArgumentException("reflected type is null", nameof(methodInfo));
        var name = methodInfo.Name;
        var parameterTypes = methodInfo.GetParameters()
                                       .Select(item => item.ParameterType)
                                       .ToArray();
        return GenerateName(returnType, reflectedType, name, parameterTypes);
    }

    public static string GenerateName(MethodInfo methodInfo, Type serviceType)
    {
        var returnType = methodInfo.ReturnType;
        var name = methodInfo.Name;
        var parameterTypes = methodInfo.GetParameters()
                                       .Select(item => item.ParameterType)
                                       .ToArray();
        return GenerateName(returnType, serviceType, name, parameterTypes);
    }

    public static string GenerateName(
        Type returnType, Type reflectedType, string methodName, params Type[] parameterTypes)
    {
        var parameterTypeNames = string.Join<Type>(", ", parameterTypes);
        return $"{returnType} {reflectedType}.{methodName}({parameterTypeNames})";
    }

    public static bool IsMethodOneWay(MethodInfo methodInfo)
    {
        return methodInfo.ReturnType == typeof(void);
    }

    public static bool IsMethodCancelable(MethodInfo methodInfo)
    {
        var parameterInfos = methodInfo.GetParameters();
        if (parameterInfos.Length > 0
            && parameterInfos[^1].ParameterType == typeof(CancellationToken))
        {
            return true;
        }

        return false;
    }

    public static Type[] GetParameterTypes(MethodInfo methodInfo)
    {
        var query = from parameterInfo in methodInfo.GetParameters()
                    let parameterType = parameterInfo.ParameterType
                    where parameterType != typeof(CancellationToken)
                    select parameterType;
        return [.. query];
    }

    public static ParameterInfo[] GetParameterInfos(MethodInfo methodInfo)
    {
        var query = from parameterInfo in methodInfo.GetParameters()
                    let parameterType = parameterInfo.ParameterType
                    where parameterType != typeof(CancellationToken)
                    select parameterInfo;
        return [.. query];
    }
}
