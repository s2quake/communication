// <copyright file="ServerContext.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System.ComponentModel.Composition;

namespace JSSoft.Communication.ConsoleApp;

[Export(typeof(IServiceContext))]
[method: ImportingConstructor]
internal sealed class ServerContext([ImportMany] IService[] services)
    : Communication.ServerContext(services)
{
}
