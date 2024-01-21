using System.Net;
using System.Net.Sockets;

namespace JSSoft.Communication.Tests;

static class EndPointUtility
{
    public static EndPoint GetEndPoint()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return new DnsEndPoint(ServiceContextBase.DefaultHost, ((IPEndPoint)listener.LocalEndpoint).Port);
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
