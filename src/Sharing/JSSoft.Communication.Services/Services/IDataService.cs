// <copyright file="IDataService.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;

namespace JSSoft.Communication.Services;

public interface IDataService
{
    Task<DateTime> CreateDataBaseAsync(string dataBaseName, CancellationToken cancellationToken);
}
