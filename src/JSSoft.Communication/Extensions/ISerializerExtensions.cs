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
using System.Threading;

namespace JSSoft.Communication.Extensions;

static class ISerializerExtensions
{
    public static string[] SerializeMany(this ISerializer @this, Type[] types, object?[] args)
    {
        var items = new string[args.Length];
        for (var i = 0; i < args.Length; i++)
        {
            var type = types[i];
            var value = args[i];
            items[i] = @this.Serialize(type, value);
        }
        return items;
    }

    public static object?[] DeserializeMany(this ISerializer @this, Type[] types, string[] datas)
    {
        var items = new object?[datas.Length];
        for (var i = 0; i < datas.Length; i++)
        {
            var type = types[i];
            var value = datas[i];
            items[i] = @this.Deserialize(type, value);
        }
        return items;
    }

    public static object?[] DeserializeMany(this ISerializer @this, Type[] types, string[] datas, CancellationToken? cancellationToken)
    {
        var length = cancellationToken != null ? datas.Length + 1 : datas.Length;
        var items = new object?[length];
        for (var i = 0; i < datas.Length; i++)
        {
            var type = types[i];
            var value = datas[i];
            items[i] = @this.Deserialize(type, value);
        }
        if (cancellationToken != null)
        {
            items[datas.Length] = cancellationToken;
        }
        return items;
    }
}
