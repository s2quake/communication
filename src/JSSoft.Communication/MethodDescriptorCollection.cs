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
    private readonly Dictionary<string, MethodDescriptor> _descriptorByName;

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
            _descriptorByName = methodDescriptors.ToDictionary(item => item.Name);
        }
        else
        {
            _descriptorByName = [];
        }
    }

    public int Count => _descriptorByName.Count;

    public MethodDescriptor this[string name] => _descriptorByName[name];

    public bool Contains(string name) => _descriptorByName.ContainsKey(name);

    public IEnumerator<MethodDescriptor> GetEnumerator()
        => _descriptorByName.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => _descriptorByName.Values.GetEnumerator();
}
