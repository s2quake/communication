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

public abstract class ServiceBase(Type serverType, Type clientType) : IService
{
    public Type ServerType { get; } = ServiceUtility.ValidateServerType(serverType);

    public Type ClientType { get; } = ServiceUtility.ValidateClientType(clientType);

    public string Name { get; } = serverType.Name;

    protected abstract MethodDescriptorBase? CreateMethodDescriptor(MethodInfo methodInfo);

    protected abstract object CreateInstance(IPeer peer, object obj);

    protected abstract void DestroyInstance(IPeer peer, object obj);

    #region IService

    object IService.CreateInstance(IPeer peer, object obj) => CreateInstance(peer, obj);

    void IService.DestroyInstance(IPeer peer, object obj) => DestroyInstance(peer, obj);

    MethodDescriptorBase? IService.CreateMethodDescriptor(MethodInfo methodInfo)
        => CreateMethodDescriptor(methodInfo);

    #endregion
}
