// <copyright file="DataCommand.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using JSSoft.Commands;
using JSSoft.Communication.ConsoleApp;
using JSSoft.Communication.Services;

namespace JSSoft.Communication.Commands;

[Export(typeof(ICommand))]
[method: ImportingConstructor]
internal class DataCommand(Application application, IDataService dataService) : CommandMethodBase
{
    private readonly Application _application = application;
    private readonly IDataService _dataService = dataService;

    public override bool IsEnabled => _application.UserToken != Guid.Empty;

    [CommandMethod]
    public Task CreateAsync(string dataBaseName, CancellationToken cancellationToken)
    {
        return _dataService.CreateDataBaseAsync(dataBaseName, cancellationToken);
    }

    protected override bool IsMethodEnabled(CommandMethodDescriptor memberDescriptor)
    {
        return _application.UserToken != Guid.Empty;
    }
}
