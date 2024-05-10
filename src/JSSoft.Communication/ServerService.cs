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

namespace JSSoft.Communication;

[Service(IsServer = true)]
public class ServerService<TServer, TClient>
    : ServiceBase
    where TServer : class
    where TClient : class
{
    private readonly TServer _server;
    private TClient? _client;

    public ServerService(TServer server)
        : base(typeof(TServer), typeof(TClient))
        => _server = server;

    public ServerService()
        : base(typeof(TServer), typeof(TClient))
    {
        if (typeof(TServer).IsAssignableFrom(this.GetType()) == false)
            throw new InvalidOperationException("This type must be implemented by TServer.");

        _server = (this as TServer)!;
    }

    public TClient Client => _client ?? throw new InvalidOperationException("Client is not created.");

    protected virtual TServer CreateServer(IPeer peer) => _server;

    protected virtual void DestroyServer(IPeer peer, TServer server)
    {
    }

    protected override sealed object CreateInstance(IPeer peer, object obj)
    {
        _client = (TClient)obj;
        return CreateServer(peer);
    }

    protected override sealed void DestroyInstance(IPeer peer, object obj)
    {
        DestroyServer(peer, (TServer)obj);
        _client = null;
    }
}

[Service(IsServer = true)]
public class ServerService<TServer> : ServiceBase
    where TServer : class
{
    private readonly TServer? _server;

    public ServerService(TServer server)
        : base(serverType: typeof(TServer), clientType: typeof(void))
        => _server = server;

    public ServerService()
        : base(serverType: typeof(TServer), clientType: typeof(void))
    {
        if (typeof(TServer).IsAssignableFrom(this.GetType()) == false)
            throw new InvalidOperationException("This type must be implemented by TServer.");

        _server = this as TServer;
    }

    protected virtual TServer CreateServer(IPeer peer) => _server!;

    protected virtual void DestroyServer(IPeer peer, TServer server)
    {
    }

    protected override sealed object CreateInstance(IPeer peer, object obj)
        => CreateServer(peer);

    protected override sealed void DestroyInstance(IPeer peer, object obj)
        => DestroyServer(peer, (TServer)obj);
}
