// <copyright file="MethodDescriptor.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using static JSSoft.Communication.MethodUtility;

namespace JSSoft.Communication;

public class MethodDescriptor
{
    public MethodDescriptor(MethodInfo methodInfo)
    {
        MethodInfo = methodInfo;
        ParameterTypes = GetParameterTypes(methodInfo);
        ReturnType = methodInfo.ReturnType;
        if (ReturnType == typeof(Task))
        {
            ReturnType = typeof(void);
            IsAsync = true;
        }
        else if (ReturnType.IsSubclassOf(typeof(Task)) == true)
        {
            ReturnType = ReturnType.GetGenericArguments()[0];
            IsAsync = true;
        }

        Name = GenerateName(methodInfo);
        ShortName = methodInfo.Name;
        IsOneWay = IsMethodOneWay(methodInfo);
        IsCancelable = IsMethodCancelable(methodInfo);
        Verify(methodInfo);
    }

    public virtual string Name { get; }

    public virtual string ShortName { get; }

    public virtual Type[] ParameterTypes { get; }

    public virtual Type ReturnType { get; }

    public virtual bool IsAsync { get; }

    public virtual bool IsOneWay { get; }

    public virtual bool IsCancelable { get; }

    public MethodInfo MethodInfo { get; }

    // "async" methods should not return "void"
#pragma warning disable S3168
    internal async void InvokeOneWay(
        IServiceProvider serviceProvider, object instance, object?[] args)
    {
        try
        {
            await InvokeAsync(instance, args);
        }
        catch
        {
            // do nothing
        }
    }
#pragma warning restore S3168

    internal async Task<InvokeResult> InvokeAsync(
        IServiceProvider serviceProvider, object instance, object?[] args)
    {
        try
        {
            var (valueType, value) = await InvokeAsync(instance, args);
            return new InvokeResult
            {
                AssemblyQualifiedName = string.Empty,
                ValueType = valueType,
                Value = value,
            };
        }
        catch (TargetInvocationException e)
        {
            var exception = e.InnerException ?? e;
            return new InvokeResult
            {
                AssemblyQualifiedName = exception.GetType().AssemblyQualifiedName!,
                ValueType = exception.GetType(),
                Value = exception,
            };
        }
        catch (Exception e)
        {
            return new InvokeResult
            {
                AssemblyQualifiedName = e.GetType().AssemblyQualifiedName!,
                ValueType = e.GetType(),
                Value = e,
            };
        }
    }

    private static void Verify(MethodInfo methodInfo)
    {
        var parameterInfos = methodInfo.GetParameters();
        var isAsync = typeof(Task).IsAssignableFrom(methodInfo.ReturnType);
        if (isAsync == true)
        {
            if (parameterInfos.Count(item => item.ParameterType == typeof(CancellationToken)) > 1)
            {
                var message = $"""
                    In method '{methodInfo}', only one parameter of type 
                    {nameof(CancellationToken)} should be defined.
                    """;
                throw new InvalidOperationException(message);
            }

            if (parameterInfos.Count(item => item.ParameterType == typeof(CancellationToken)) == 1
                && parameterInfos.Last().ParameterType != typeof(CancellationToken))
            {
                var message = $"""
                    The last parameter of method '{methodInfo}' must be of 
                    type {nameof(CancellationToken)}.
                    """;
                throw new InvalidOperationException(message);
            }
        }
        else if (methodInfo.ReturnType == typeof(void))
        {
            if (parameterInfos.Any(item => item.ParameterType == typeof(CancellationToken)) == true)
            {
                var message = $"""
                    The {nameof(CancellationToken)} type cannot be used in method '{methodInfo}'.
                    """;
                throw new InvalidOperationException(message);
            }
        }
        else
        {
            var message = $"""
                The return type of method '{methodInfo}' must be 
                '{typeof(Task)}' or '{typeof(void)}'.
                """;
            throw new InvalidOperationException(message);
        }
    }

    private async Task<(Type ValueType, object? Value)> InvokeAsync(
        object? instance, object?[] args)
    {
        var value = await Task.Run(() => MethodInfo.Invoke(instance, args));
        var valueType = MethodInfo.ReturnType;
        if (value is Task task)
        {
            await task;
            var taskType = task.GetType();
            if (taskType.GetGenericArguments().Length > 0)
            {
                var propertyInfo = taskType.GetProperty(nameof(Task<object>.Result));
                value = propertyInfo!.GetValue(task);
                valueType = propertyInfo.PropertyType;
            }
            else
            {
                value = null;
                valueType = typeof(void);
            }
        }

        return (valueType, value);
    }
}
