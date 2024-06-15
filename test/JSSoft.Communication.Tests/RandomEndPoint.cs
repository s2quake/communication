// <copyright file="RandomEndPoint.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System.Net;
using System.Net.Sockets;

namespace JSSoft.Communication.Tests;

internal sealed class RandomEndPoint : IDisposable
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
        ObjectDisposedException.ThrowIf(_isDisposed, this);

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
