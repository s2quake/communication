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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace JSSoft.Communication;

public static class EndPointUtility
{
    public static (string host, int port) GetElements(EndPoint endPoint)
    {
        if (endPoint is DnsEndPoint dnsEndPoint)
        {
            return (dnsEndPoint.Host, dnsEndPoint.Port);
        }
        else if (endPoint is IPEndPoint iPEndPoint)
        {
            return ($"{iPEndPoint.Address}", iPEndPoint.Port);
        }
        throw new NotSupportedException($"'{endPoint}' is not supported.");
    }

    public static string ToString(EndPoint endPoint)
    {
        if (endPoint is DnsEndPoint dnsEndPoint)
        {
            return $"{dnsEndPoint.Host}:{dnsEndPoint.Port}";
        }
        else if (endPoint is IPEndPoint iPEndPoint)
        {
            return $"{iPEndPoint.Address}:{iPEndPoint.Port}";
        }
        throw new NotSupportedException($"'{endPoint}' is not supported.");
    }

    public static EndPoint Parse(string text)
    {
        var items = text.Split(':');
        if (IPAddress.TryParse(items[0], out var address) == true)
        {
            return new IPEndPoint(address, int.Parse(items[1]));
        }
        else if (items.Length == 2)
        {
            return new DnsEndPoint(items[0], int.Parse(items[1]));
        }

        throw new NotSupportedException($"'{text}' is not supported.");
    }

    public static bool TryParse(string text, [MaybeNullWhen(false)] out EndPoint endPoint)
    {
        try
        {
            endPoint = Parse(text);
            return true;
        }
        catch
        {
            endPoint = null;
            return false;
        }
    }

#if NETSTANDARD
    internal static global::Grpc.Core.ServerPort GetServerPort(EndPoint endPoint, global::Grpc.Core.ServerCredentials credentials)
    {
        if (endPoint is DnsEndPoint dnsEndPoint)
        {
            return new(dnsEndPoint.Host, dnsEndPoint.Port, credentials);
        }
        else if (endPoint is IPEndPoint iPEndPoint)
        {
            return new($"{iPEndPoint.Address}", iPEndPoint.Port, credentials);
        }
        throw new NotSupportedException($"'{endPoint}' is not supported.");
    }
#endif

    internal static IPEndPoint ConvertToIPEndPoint(EndPoint endPoint)
    {
        var (host, port) = GetElements(endPoint);
        if (IPAddress.TryParse(host, out var address) == true)
        {
            return new IPEndPoint(address, port);
        }

        var addresses = Dns.GetHostAddresses(host)
                           .Where(item => item.AddressFamily == AddressFamily.InterNetwork)
                           .ToArray();
        if (addresses.Length > 0)
        {
            return new IPEndPoint(addresses[0], port);
        }

        throw new NotSupportedException($"'{host}' is not supported.");
    }
}
