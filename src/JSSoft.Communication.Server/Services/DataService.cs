// MIT License
// 
// Copyright (c) 2024 Jeesu Choi
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using JSSoft.Communication.Threading;

namespace JSSoft.Communication.Services;

[Export(typeof(IService))]
[Export(typeof(IDataService))]
sealed class DataService : ServerService<IDataService>, IDataService, IDisposable
{
    private readonly HashSet<string> _dataBases = [];
    private Dispatcher? _dispatcher;

    public DataService()
    {
        _dispatcher = new Dispatcher(this);
    }

    public Task<DateTime> CreateDataBaseAsync(string dataBaseName, CancellationToken cancellationToken)
    {
        return Dispatcher.InvokeAsync(() =>
        {
            if (_dataBases.Contains(dataBaseName) == true)
                throw new ArgumentNullException(nameof(dataBaseName));
            _dataBases.Add(dataBaseName);
            return DateTime.UtcNow;
        });
    }

    public void Dispose()
    {
        if (_dispatcher == null)
            throw new ObjectDisposedException($"{this}");

        _dispatcher.Dispose();
        _dispatcher = null;
        GC.SuppressFinalize(this);
    }

    public Dispatcher Dispatcher => _dispatcher ?? throw new ObjectDisposedException($"{this}");
}