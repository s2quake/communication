// <copyright file="DataService.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using JSSoft.Communication.Services;

namespace JSSoft.Communication.Client.Services;

[Export(typeof(IService))]
[Export(typeof(IDataService))]
internal sealed class DataService : ClientService<IDataService>, IDataService
{
    public Task<DateTime> CreateDataBaseAsync(
        string dataBaseName, CancellationToken cancellationToken)
    {
        return Server.CreateDataBaseAsync(dataBaseName, cancellationToken);
    }
}
