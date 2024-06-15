// <copyright file="ServerContext.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

namespace JSSoft.Communication;

[ServiceContext(IsServer = true)]
public class ServerContext(params IService[] services)
    : ServiceContextBase(services)
{
}
