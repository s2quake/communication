// <copyright file="ServiceBase.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;

namespace JSSoft.Communication;

public abstract class ServiceBase(Type serverType, Type clientType) : IService
{
    public Type ServerType { get; } = ServiceUtility.ValidateServerType(serverType);

    public Type ClientType { get; } = ServiceUtility.ValidateClientType(clientType);

    public string Name { get; } = serverType.Name;

    object IService.CreateInstance(IPeer peer, object obj) => CreateInstance(peer, obj);

    void IService.DestroyInstance(IPeer peer, object obj) => DestroyInstance(peer, obj);

    protected abstract object CreateInstance(IPeer peer, object obj);

    protected abstract void DestroyInstance(IPeer peer, object obj);
}
