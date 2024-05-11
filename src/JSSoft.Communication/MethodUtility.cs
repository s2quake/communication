// MIT License
// 
// Copyright (c) 2024 Jeesu Choi
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace JSSoft.Communication;

public static class MethodUtility
{
    public static string GenerateName(MethodInfo methodInfo)
    {
        var parameterTypes = methodInfo.GetParameters().Select(item => item.ParameterType).ToArray();
        return GenerateName(methodInfo.ReturnType, methodInfo.ReflectedType!, methodInfo.Name, parameterTypes);
    }

    public static string GenerateName(MethodInfo methodInfo, Type serviceType)
    {
        var parameterTypes = methodInfo.GetParameters().Select(item => item.ParameterType).ToArray();
        return GenerateName(methodInfo.ReturnType, serviceType, methodInfo.Name, parameterTypes);
    }

    public static string GenerateName(Type returnType, Type reflectedType, string methodName, params Type[] parameterTypes)
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
        if (parameterInfos.Length > 0 && parameterInfos[parameterInfos.Length - 1].ParameterType == typeof(CancellationToken))
            return true;
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
