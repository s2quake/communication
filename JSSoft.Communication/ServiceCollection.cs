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

public class ServiceCollection(IEnumerable<IService> services) : IReadOnlyDictionary<string, IService>
{
    private readonly Dictionary<string, IService> _serviceByName = services.ToDictionary(item => item.Name);

    public IService this[string key] => _serviceByName[key];

    public IEnumerable<string> Keys => _serviceByName.Keys;

    public IEnumerable<IService> Values => _serviceByName.Values;

    public int Count => _serviceByName.Count;

    public bool ContainsKey(string key) => _serviceByName.ContainsKey(key);

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out IService value)
        => _serviceByName.TryGetValue(key, out value);

    #region IEnumerable

    IEnumerator<KeyValuePair<string, IService>> IEnumerable<KeyValuePair<string, IService>>.GetEnumerator()
        => _serviceByName.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => _serviceByName.GetEnumerator();

    #endregion
}
