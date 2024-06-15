// <copyright file="DataService.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using JSSoft.Communication.Services;
using JSSoft.Communication.Threading;

namespace JSSoft.Communication.Server.Services;

[Export(typeof(IService))]
[Export(typeof(IDataService))]
internal sealed class DataService : ServerService<IDataService>, IDataService, IDisposable
{
    private readonly HashSet<string> _dataBases = [];
    private Dispatcher? _dispatcher;

    public DataService()
    {
        _dispatcher = new Dispatcher(this);
    }

    public Dispatcher Dispatcher => _dispatcher ?? throw new ObjectDisposedException($"{this}");

    public Task<DateTime> CreateDataBaseAsync(
        string dataBaseName, CancellationToken cancellationToken)
    {
        return Dispatcher.InvokeAsync(() => CreateDataBase(dataBaseName));

        DateTime CreateDataBase(string dataBaseName)
        {
            if (_dataBases.Contains(dataBaseName) == true)
            {
                throw new ArgumentException("The database already exists.", nameof(dataBaseName));
            }

            _dataBases.Add(dataBaseName);
            return DateTime.UtcNow;
        }
    }

    public void Dispose()
    {
        ObjectDisposedException.ThrowIf(_dispatcher == null, this);

        _dispatcher.Dispose();
        _dispatcher = null;
        GC.SuppressFinalize(this);
    }
}
