// <copyright file="IService.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;

namespace JSSoft.Communication;

public interface IService
{
    Type ServerType { get; }

    Type ClientType { get; }

    string Name { get; }

    object CreateInstance(IPeer peer, object obj);

    void DestroyInstance(IPeer peer, object obj);
}
