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

    public bool Contains(string name) => _discriptorByName.ContainsKey(name);

    public MethodDescriptor this[string name] => _discriptorByName[name];

    #region IEnumerator

    public IEnumerator<MethodDescriptor> GetEnumerator()
        => _discriptorByName.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => _discriptorByName.Values.GetEnumerator();

    #endregion
}
