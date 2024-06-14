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
            ReturnType = ReturnType.GetGenericArguments().First();
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

    private static void Verify(MethodInfo methodInfo)
    {
        var parameterInfos = methodInfo.GetParameters();
        var isAsync = typeof(Task).IsAssignableFrom(methodInfo.ReturnType);
        if (isAsync == true)
        {
            if (parameterInfos.Count(item => item.ParameterType == typeof(CancellationToken)) > 1)
                throw new InvalidOperationException($"In method '{methodInfo}', only one parameter of type {nameof(CancellationToken)} should be defined.");
            if (parameterInfos.Count(item => item.ParameterType == typeof(CancellationToken)) == 1 &&
                parameterInfos.Last().ParameterType != typeof(CancellationToken))
                throw new InvalidOperationException($"The last parameter of method '{methodInfo}' must be of type {nameof(CancellationToken)}.");
        }
        else if (methodInfo.ReturnType == typeof(void))
        {
            if (parameterInfos.Any(item => item.ParameterType == typeof(CancellationToken)) == true)
                throw new InvalidOperationException($"The {nameof(CancellationToken)} type cannot be used in method '{methodInfo}'.");
        }
        else
        {
            throw new InvalidOperationException($"The return type of method '{methodInfo}' must be '{typeof(Task)}' or '{typeof(void)}'.");
        }
    }

    internal async void InvokeOneWay(IServiceProvider serviceProvider, object instance, object?[] args)
    {
        try
        {
            await InvokeAsync(instance, args);
        }
        catch
        {
        }
    }

    internal async Task<(string, Type, object?)> InvokeAsync(IServiceProvider serviceProvider, object instance, object?[] args)
    {
        try
        {
            var (type, value) = await InvokeAsync(instance, args);
            return (string.Empty, type, value);
        }
        catch (TargetInvocationException e)
        {
            var exception = e.InnerException ?? e;
            return (exception.GetType().AssemblyQualifiedName!, exception.GetType(), exception);
        }
        catch (Exception e)
        {
            return (e.GetType().AssemblyQualifiedName!, e.GetType(), e);
        }
    }

    private async Task<(Type, object?)> InvokeAsync(object? instance, object?[] args)
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
