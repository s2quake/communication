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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;

namespace JSSoft.Communication;

sealed class ServiceInstanceBuilder
{
    private const string ns = "JSSoft.Communication.Runtime";
    private readonly Dictionary<string, Type> _typeByName = [];
    private readonly AssemblyBuilder _assemblyBuilder;
    private readonly ModuleBuilder _moduleBuilder;

    internal ServiceInstanceBuilder()
    {
        AssemblyName = new AssemblyName(ns);
        _assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(AssemblyName, AssemblyBuilderAccess.RunAndCollect);
        _moduleBuilder = _assemblyBuilder.DefineDynamicModule(AssemblyName.Name!);
    }

    internal static ServiceInstanceBuilder? Create()
    {
        try
        {
            return new ServiceInstanceBuilder();
        }
        catch
        {
            return null;
        }
    }

    public AssemblyName AssemblyName { get; }

    public Type CreateType(string name, Type baseType, Type interfaceType)
    {
        var fullName = $"{AssemblyName}.{name}";
        if (_typeByName.ContainsKey(fullName) != true)
        {
            var type = CreateType(_moduleBuilder, name, baseType, interfaceType);
            _typeByName.Add(fullName, type);
        }
        return _typeByName[fullName];
    }

    private static Type CreateType(ModuleBuilder moduleBuilder, string typeName, Type baseType, Type interfaceType)
    {
        var typeBuilder = moduleBuilder.DefineType(typeName, TypeAttributes.Class | TypeAttributes.Public, baseType, [interfaceType]);
        var methodInfos = interfaceType.GetMethods();
        foreach (var methodInfo in methodInfos)
        {
            var operationContractAttribute = (ServerMethodAttribute)Attribute.GetCustomAttribute(methodInfo, typeof(ServerMethodAttribute))!;
            var isOneWay = MethodDescriptor.IsMethodOneWay(methodInfo);
            var returnType = methodInfo.ReturnType;
            if (returnType == typeof(Task))
            {
                CreateInvokeAsync(typeBuilder, methodInfo, InstanceBase.InvokeAsyncMethod);
            }
            else if (returnType.IsSubclassOf(typeof(Task)) == true)
            {
                CreateInvokeAsync(typeBuilder, methodInfo, InstanceBase.InvokeGenericAsyncMethod);
            }
            else if (returnType == typeof(void))
            {
                if (isOneWay == true)
                    CreateInvoke(typeBuilder, methodInfo, InstanceBase.InvokeOneWayMethod);
                else
                    CreateInvoke(typeBuilder, methodInfo, InstanceBase.InvokeMethod);
            }
            else
            {
                CreateInvoke(typeBuilder, methodInfo, InstanceBase.InvokeGenericMethod);
            }
        }

        return typeBuilder.CreateType();
    }

    private static void CreateInvoke(TypeBuilder typeBuilder, MethodInfo methodInfo, string methodName)
    {
        var parameterInfos = methodInfo.GetParameters();
        var parameterTypes = parameterInfos.Select(i => i.ParameterType).ToArray();
        var returnType = methodInfo.ReturnType;
        var methodAttributes = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig;
        var methodBuilder = typeBuilder.DefineMethod(methodInfo.Name, methodAttributes, CallingConventions.Standard, returnType, parameterTypes);
        var invokeMethod = FindInvokeMethod(typeBuilder.BaseType!, methodName, returnType);
        var typeofMethod = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle))!;

        for (var i = 0; i < parameterInfos.Length; i++)
        {
            var pb = methodBuilder.DefineParameter(i, ParameterAttributes.Lcid, parameterInfos[i].Name);
        }

        var il = methodBuilder.GetILGenerator();
        il.DeclareLocal(typeof(Type[]));
        il.DeclareLocal(typeof(object[]));
        if (returnType != typeof(void))
        {
            il.DeclareLocal(returnType);
        }
        il.Emit(OpCodes.Nop);
        il.EmitLdc_I4(parameterInfos.Length);
        il.Emit(OpCodes.Newarr, typeof(Type));
        for (var i = 0; i < parameterInfos.Length; i++)
        {
            var item = parameterInfos[i];
            il.Emit(OpCodes.Dup);
            il.EmitLdc_I4(i);
            il.Emit(OpCodes.Ldtoken, parameterTypes[i]);
            il.Emit(OpCodes.Call, typeofMethod);
            il.Emit(OpCodes.Stelem_Ref);
        }
        il.Emit(OpCodes.Stloc_0);
        il.EmitLdc_I4(parameterInfos.Length);
        il.Emit(OpCodes.Newarr, typeof(object));
        for (var i = 0; i < parameterInfos.Length; i++)
        {
            var item = parameterInfos[i];
            il.Emit(OpCodes.Dup);
            il.EmitLdc_I4(i);
            il.EmitLdarg(i + 1);
            if (item.ParameterType.IsClass != true)
            {
                il.Emit(OpCodes.Box, parameterTypes[i]);
            }
            il.Emit(OpCodes.Stelem_Ref);
        }
        il.Emit(OpCodes.Stloc_1);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldstr, MethodDescriptor.GenerateName(methodInfo));
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Ldloc_1);
        il.Emit(OpCodes.Call, invokeMethod);
        il.Emit(OpCodes.Nop);
        il.Emit(OpCodes.Ret);
    }

    private static void CreateInvokeAsync(TypeBuilder typeBuilder, MethodInfo methodInfo, string methodName)
    {
        var isCancelable = MethodDescriptor.IsMethodCancelable(methodInfo);
        var parameterInfos = methodInfo.GetParameters();
        var parameterTypes = parameterInfos.Select(i => i.ParameterType).ToArray();
        var returnType = methodInfo.ReturnType;
        var methodAttributes = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig;
        var methodBuilder = typeBuilder.DefineMethod(methodInfo.Name, methodAttributes, returnType, parameterTypes);
        var invokeMethod = FindInvokeMethod(typeBuilder.BaseType!, methodName, returnType);
        var typeofMethod = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle))!;

        for (var i = 0; i < parameterInfos.Length; i++)
        {
            methodBuilder.DefineParameter(i, ParameterAttributes.None, parameterInfos[i].Name);
        }

        var parameterLength = isCancelable == true ? parameterInfos.Length - 1 : parameterInfos.Length;
        var il = methodBuilder.GetILGenerator();
        il.DeclareLocal(typeof(Type[]));
        il.DeclareLocal(typeof(object[]));
        il.DeclareLocal(returnType);
        il.Emit(OpCodes.Nop);
        il.EmitLdc_I4(parameterLength);
        il.Emit(OpCodes.Newarr, typeof(Type));
        for (var i = 0; i < parameterLength; i++)
        {
            var item = parameterInfos[i];
            il.Emit(OpCodes.Dup);
            il.EmitLdc_I4(i);
            il.Emit(OpCodes.Ldtoken, parameterTypes[i]);
            il.Emit(OpCodes.Call, typeofMethod);
            il.Emit(OpCodes.Stelem_Ref);
        }
        il.Emit(OpCodes.Stloc_0);
        il.EmitLdc_I4(parameterLength);
        il.Emit(OpCodes.Newarr, typeof(object));
        for (var i = 0; i < parameterLength; i++)
        {
            var item = parameterInfos[i];
            il.Emit(OpCodes.Dup);
            il.EmitLdc_I4(i);
            il.EmitLdarg(i + 1);
            if (item.ParameterType.IsClass != true)
            {
                il.Emit(OpCodes.Box, parameterTypes[i]);
            }
            il.Emit(OpCodes.Stelem_Ref);
        }
        il.Emit(OpCodes.Stloc_1);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldstr, MethodDescriptor.GenerateName(methodInfo));
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Ldloc_1);
        if (isCancelable == true)
        {
            il.EmitLdarg(parameterLength + 1);
        }
        else
        {
            il.Emit(OpCodes.Call, typeof(CancellationToken).GetMethod("get_None", BindingFlags.Public | BindingFlags.Static)!);
        }
        il.Emit(OpCodes.Call, invokeMethod);
        il.Emit(OpCodes.Nop);
        il.Emit(OpCodes.Ret);
    }

    private static MethodInfo FindInvokeMethod(Type baseType, string methodName, Type returnType)
    {
        var methodInfos = baseType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var item in methodInfos)
        {
            if (item.GetCustomAttribute<InstanceMethodAttribute>() is InstanceMethodAttribute attr && attr.MethodName == methodName)
            {
                if (item.IsGenericMethod == true)
                {
                    if (returnType.IsGenericType == true)
                        return item.MakeGenericMethod(returnType.GetGenericArguments());
                    else
                        return item.MakeGenericMethod(returnType);
                }
                return item;
            }
        }
        throw new NotImplementedException();
    }

    private static MethodInfo FindInvokeMethod(MethodInfo[] methodInfos, string methodName)
    {
        foreach (var item in methodInfos)
        {
            if (item.GetCustomAttribute<InstanceMethodAttribute>() is InstanceMethodAttribute attr && attr.MethodName == methodName)
            {
                return item;
            }
        }
        throw new NotImplementedException();
    }
}
