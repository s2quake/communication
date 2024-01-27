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
using System.Reflection;

namespace JSSoft.Communication;

public static class ServiceUtility
{
    public static Type ValidateServerType(Type ServiceType)
    {
        if (ServiceType.IsInterface != true)
            throw new InvalidOperationException("Server type must be interface.");

        if (IsNestedPublicType(ServiceType) != true && IsPublicType(ServiceType) != true && IsInternalType(ServiceType) != true)
            throw new InvalidOperationException($"'{ServiceType.Name}' must be public or internal.");
        return ServiceType;
    }

    public static Type ValidateClientType(Type CallbackType)
    {
        if (CallbackType != typeof(void))
        {
            if (CallbackType.IsInterface != true)
                throw new InvalidOperationException("Client type must be interface.");
            if (IsNestedPublicType(CallbackType) != true && IsPublicType(CallbackType) != true && IsInternalType(CallbackType) != true)
                throw new InvalidOperationException($"'{CallbackType.Name}' must be public or internal.");
        }
        return CallbackType;
    }

    public static bool IsNestedPublicType(Type type)
        => type.IsNested == true && type.IsNestedPublic == true;

    public static bool IsPublicType(Type type)
        => type.IsVisible == true && type.IsPublic == true && type.IsNotPublic != true;

    public static bool IsInternalType(Type type)
        => type.IsVisible != true && type.IsPublic != true && type.IsNotPublic == true;

    public static bool IsServer(IService service)
    {
        if (service.GetType().GetCustomAttribute(typeof(ServiceAttribute)) is ServiceAttribute serviceAttribute)
        {
            return serviceAttribute.IsServer;
        }
        return false;
    }
}
