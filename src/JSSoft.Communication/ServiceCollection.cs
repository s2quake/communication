// <copyright file="ServiceCollection.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace JSSoft.Communication;

public class ServiceCollection(IEnumerable<IService> services)
    : IReadOnlyDictionary<string, IService>
{
    private readonly Dictionary<string, IService> _serviceByName
        = services.ToDictionary(item => item.Name);

    public IEnumerable<string> Keys => _serviceByName.Keys;

    public IEnumerable<IService> Values => _serviceByName.Values;

    public int Count => _serviceByName.Count;

    public IService this[string key] => _serviceByName[key];

    public bool ContainsKey(string key) => _serviceByName.ContainsKey(key);

#if NETSTANDARD
    public bool TryGetValue(string key, out IService value)
        => _serviceByName.TryGetValue(key, out value);
#elif NET
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out IService value)
        => _serviceByName.TryGetValue(key, out value);
#endif

    public IEnumerator<KeyValuePair<string, IService>> GetEnumerator()
        => _serviceByName.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => _serviceByName.GetEnumerator();
}
