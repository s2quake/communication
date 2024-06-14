// <copyright file="ClientContext.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System.ComponentModel.Composition;

namespace JSSoft.Communication.Client;

[Export(typeof(IServiceContext))]
[method: ImportingConstructor]
internal sealed class ClientContext([ImportMany] IService[] services)
    : Communication.ClientContext(services)
{
}
