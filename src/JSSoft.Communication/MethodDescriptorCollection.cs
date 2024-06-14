// <copyright file="MethodDescriptorCollection.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace JSSoft.Communication;

public sealed class MethodDescriptorCollection : IEnumerable<MethodDescriptor>
{
    private readonly Dictionary<string, MethodDescriptor> _discriptorByName;

    internal MethodDescriptorCollection(Type type)
    {
        if (type != typeof(void) && type.IsInterface != true)
        {
            throw new ArgumentException($"'{type.Name}' must be interface.");
        }

        if (type != typeof(void))
        {
            var methodInfos = type.GetMethods();
            var methodDescriptors = methodInfos.Select(item => new MethodDescriptor(item));
            _discriptorByName = methodDescriptors.ToDictionary(item => item.Name);
        }
        else
        {
            _discriptorByName = [];
        }
    }

    public int Count => _discriptorByName.Count;

    public MethodDescriptor this[string name] => _discriptorByName[name];

    public bool Contains(string name) => _discriptorByName.ContainsKey(name);

    public IEnumerator<MethodDescriptor> GetEnumerator()
        => _discriptorByName.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => _discriptorByName.Values.GetEnumerator();
}
