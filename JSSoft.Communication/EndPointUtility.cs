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
using System.Net;
using Grpc.Core;

namespace JSSoft.Communication;

public static class EndPointUtility
{
    public static (string host, int port) GetTarget(EndPoint endPoint)
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

    public static string GetString(EndPoint endPoint)
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

    public static EndPoint GetEndPoint(string endPoint)
    {
        var items = endPoint.Split(':');
        if (IPAddress.TryParse(items[0], out var address) == true)
        {
            return new IPEndPoint(address, int.Parse(items[1]));
        }
        else if (items.Length == 2)
        {
            return new DnsEndPoint(items[0], int.Parse(items[1]));
        }

        throw new NotSupportedException($"'{endPoint}' is not supported.");
    }

    internal static ServerPort GetServerPort(EndPoint endPoint, ServerCredentials credentials)
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
}
