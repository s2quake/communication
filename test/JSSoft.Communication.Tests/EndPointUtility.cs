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

using System.Net;
using System.Net.Sockets;

namespace JSSoft.Communication.Tests;

static class EndPointUtility
{
    private static readonly object LockObject = new();

    public static EndPoint GetEndPoint()
    {
        lock (LockObject)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            return new DnsEndPoint(ServiceContextBase.DefaultHost, ((IPEndPoint)listener.LocalEndpoint).Port);
        }
    }

    public static EndPoint[] GetEndPoints(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        var listeners = new TcpListener[count];
        var endPoints = new EndPoint[count];
        for (var i = 0; i < endPoints.Length; i++)
        {
            listeners[i] = new(IPAddress.Loopback, 0);
            listeners[i].Start();
            endPoints[i] = new DnsEndPoint(ServiceContextBase.DefaultHost, ((IPEndPoint)listeners[i].LocalEndpoint).Port);
        }
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].Stop();
        }
        return endPoints;
    }
}
