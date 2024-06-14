// <copyright file="IInstanceContext.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

namespace JSSoft.Communication;

public interface IInstanceContext
{
    PeerDescriptor CreateInstance(IPeer peer);

    void DestroyInstance(IPeer peer);
}
