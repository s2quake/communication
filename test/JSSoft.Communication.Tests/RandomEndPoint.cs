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

sealed class RandomEndPoint : IDisposable
{
    private static readonly object LockObject = new();
    private static readonly List<int> PortList = [];

    private readonly DnsEndPoint _endPoint;
    private bool _isDisposed;

    public RandomEndPoint()
    {
        _endPoint = new DnsEndPoint("localhost", GetPort());
    }

    public static implicit operator EndPoint(RandomEndPoint endPointObject)
    {
        return endPointObject._endPoint;
    }

    public override string ToString()
    {
        return $"{_endPoint.Host}:{_endPoint.Port}";
    }

    public void Dispose()
    {
        if (_isDisposed == true)
        {
            throw new ObjectDisposedException($"{this}");
        }

        lock (LockObject)
        {
            PortList.Remove(_endPoint.Port);
        }
        _isDisposed = true;
    }

    private static int GetPort()
    {
        lock (LockObject)
        {
            var port = GetRandomPort();
            while (PortList.Contains(port) == true)
            {
                port = GetRandomPort();
            }

            PortList.Add(port);
            return port;
        }
    }

    private static int GetRandomPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
