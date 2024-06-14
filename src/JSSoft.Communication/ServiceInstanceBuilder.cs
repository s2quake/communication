// <copyright file="ServiceInstanceBuilder.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

// Reflection should not be used to increase accessibility of classes, methods, or fields
#pragma warning disable S3011

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;

namespace JSSoft.Communication;

internal sealed class ServiceInstanceBuilder
{
    private const string Namespace = "JSSoft.Communication.Runtime";
    private readonly Dictionary<string, Type> _typeByName = [];
    private readonly ModuleBuilder _moduleBuilder;

    internal ServiceInstanceBuilder()
    {
        var assemblyName = new AssemblyName(Namespace);
        var access = AssemblyBuilderAccess.RunAndCollect;
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, access);
        _moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);
        AssemblyName = assemblyName;
    }

    public AssemblyName AssemblyName { get; }

    public Type CreateType(string name, Type baseType, Type interfaceType)
    {
        var fullName = $"{AssemblyName}.{name}";
        if (_typeByName.TryGetValue(fullName, out var value) != true)
        {
            var type = CreateType(_moduleBuilder, name, baseType, interfaceType);
            _typeByName.Add(fullName, type);
            value = type;
        }

        return value;
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

    private static Type CreateType(
        ModuleBuilder moduleBuilder, string typeName, Type baseType, Type interfaceType)
    {
        var typeAttrs = TypeAttributes.Class | TypeAttributes.Public;
        var typeBuilder = moduleBuilder.DefineType(typeName, typeAttrs, baseType, [interfaceType]);
        var methodInfos = interfaceType.GetMethods();
        foreach (var methodInfo in methodInfos)
        {
            var isOneWay = MethodUtility.IsMethodOneWay(methodInfo);
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
                {
                    CreateInvoke(typeBuilder, methodInfo, InstanceBase.InvokeOneWayMethod);
                }
                else
                {
                    CreateInvoke(typeBuilder, methodInfo, InstanceBase.InvokeMethod);
                }
            }
            else
            {
                CreateInvoke(typeBuilder, methodInfo, InstanceBase.InvokeGenericMethod);
            }
        }

        return typeBuilder.CreateType();
    }

    private static void CreateInvoke(
        TypeBuilder typeBuilder, MethodInfo methodInfo, string methodName)
    {
        var parameterInfos = methodInfo.GetParameters();
        var parameterTypes = parameterInfos.Select(i => i.ParameterType).ToArray();
        var returnType = methodInfo.ReturnType;
        var methodAttributes = MethodAttributes.Public | MethodAttributes.Virtual
            | MethodAttributes.Final | MethodAttributes.HideBySig;
        var methodBuilder = typeBuilder.DefineMethod(
            name: methodInfo.Name,
            attributes: methodAttributes,
            callingConvention: CallingConventions.Standard,
            returnType: returnType,
            parameterTypes: parameterTypes);
        var invokeMethod = FindInvokeMethod(typeBuilder.BaseType!, methodName, returnType);
        var typeofMethod = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle))!;

        for (var i = 0; i < parameterInfos.Length; i++)
        {
            methodBuilder.DefineParameter(i, ParameterAttributes.Lcid, parameterInfos[i].Name);
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
        il.Emit(OpCodes.Ldstr, MethodUtility.GenerateName(methodInfo));
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Ldloc_1);
        il.Emit(OpCodes.Call, invokeMethod);
        il.Emit(OpCodes.Nop);
        il.Emit(OpCodes.Ret);
    }

    private static void CreateInvokeAsync(
        TypeBuilder typeBuilder, MethodInfo methodInfo, string methodName)
    {
        var isCancelable = MethodUtility.IsMethodCancelable(methodInfo);
        var parameterInfos = methodInfo.GetParameters();
        var parameterTypes = parameterInfos.Select(i => i.ParameterType).ToArray();
        var returnType = methodInfo.ReturnType;
        var methodAttributes = MethodAttributes.Public | MethodAttributes.Virtual
            | MethodAttributes.Final | MethodAttributes.HideBySig;
        var methodBuilder = typeBuilder.DefineMethod(
            name: methodInfo.Name,
            attributes: methodAttributes,
            returnType: returnType,
            parameterTypes: parameterTypes);
        var invokeMethod = FindInvokeMethod(typeBuilder.BaseType!, methodName, returnType);
        var typeofMethod = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle))!;

        for (var i = 0; i < parameterInfos.Length; i++)
        {
            methodBuilder.DefineParameter(i, ParameterAttributes.None, parameterInfos[i].Name);
        }

        var parameterLength = isCancelable == true
            ? parameterInfos.Length - 1
            : parameterInfos.Length;
        var il = methodBuilder.GetILGenerator();
        il.DeclareLocal(typeof(Type[]));
        il.DeclareLocal(typeof(object[]));
        il.DeclareLocal(returnType);
        il.Emit(OpCodes.Nop);
        il.EmitLdc_I4(parameterLength);
        il.Emit(OpCodes.Newarr, typeof(Type));
        for (var i = 0; i < parameterLength; i++)
        {
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
        il.Emit(OpCodes.Ldstr, MethodUtility.GenerateName(methodInfo));
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Ldloc_1);
        if (isCancelable == true)
        {
            il.EmitLdarg(parameterLength + 1);
        }
        else
        {
            var bindingFlags = BindingFlags.Public | BindingFlags.Static;
            var meth = typeof(CancellationToken).GetMethod("get_None", bindingFlags)!;
            il.Emit(OpCodes.Call, meth);
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
            var attribute = item.GetCustomAttribute<InstanceMethodAttribute>();
            if (attribute is InstanceMethodAttribute instanceMethodAttribute
                && instanceMethodAttribute.MethodName == methodName)
            {
                if (item.IsGenericMethod == true)
                {
                    if (returnType.IsGenericType == true)
                    {
                        return item.MakeGenericMethod(returnType.GetGenericArguments());
                    }
                    else
                    {
                        return item.MakeGenericMethod(returnType);
                    }
                }

                return item;
            }
        }

        throw new NotSupportedException($"'{methodName}' method is not found.");
    }
}
