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

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace JSSoft.Communication;

public class ServiceHostCollection(IEnumerable<IServiceHost> serviceHosts) : IReadOnlyDictionary<string, IServiceHost>
{
    private readonly Dictionary<string, IServiceHost> _serviceHostByName = serviceHosts.ToDictionary(item => item.Name);

    public IServiceHost this[string key] => _serviceHostByName[key];

    public IEnumerable<string> Keys => _serviceHostByName.Keys;

    public IEnumerable<IServiceHost> Values => _serviceHostByName.Values;

    public int Count => _serviceHostByName.Count;

    public bool ContainsKey(string key) => _serviceHostByName.ContainsKey(key);

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out IServiceHost value)
    {
        return _serviceHostByName.TryGetValue(key, out value);
    }

    #region IEnumerable

    IEnumerator<KeyValuePair<string, IServiceHost>> IEnumerable<KeyValuePair<string, IServiceHost>>.GetEnumerator()
        => _serviceHostByName.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => _serviceHostByName.GetEnumerator();

    #endregion
}
