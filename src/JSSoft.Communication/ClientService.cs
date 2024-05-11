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
using System.Reflection;

namespace JSSoft.Communication;

[Service(IsServer = false)]
public class ClientService<TServer, TClient>
    : ServiceBase
    where TServer : class
    where TClient : class
{
    private readonly TClient _client;
    private TServer? _server;

    public ClientService(TClient client)
        : base(typeof(TServer), typeof(TClient))
        => _client = client;

    public ClientService()
        : base(typeof(TServer), typeof(TClient))
    {
        if (typeof(TClient).IsAssignableFrom(this.GetType()) == false)
            throw new InvalidOperationException($"This type must be implemented by {nameof(TClient)}.");

        _client = (this as TClient)!;
    }

    public TServer Server => _server ?? throw new InvalidOperationException("Server is not created.");

    protected virtual TClient CreateClient(IPeer peer) => _client;

    protected virtual void DestroyClient(IPeer peer, TClient client)
    {
    }

    protected override sealed object CreateInstance(IPeer peer, object obj)
    {
        _server = (TServer?)obj;
        return CreateClient(peer);
    }

    protected override sealed void DestroyInstance(IPeer peer, object obj)
    {
        DestroyClient(peer, (TClient)obj);
        _server = null;
    }
}

[Service(IsServer = false)]
public class ClientService<TServer>
    : ServiceBase
    where TServer : class
{
    private TServer? _server;

    public ClientService()
        : base(serverType: typeof(TServer), clientType: typeof(void))
    {
    }

    public TServer Server => _server ?? throw new InvalidOperationException("Server is not created.");

    protected virtual void OnServiceCreated(IPeer peer, TServer server)
    {
    }

    protected virtual void OnServiceDestroyed(IPeer peer)
    {
    }

    protected override sealed object CreateInstance(IPeer peer, object obj)
    {
        _server = (TServer)obj;
        OnServiceCreated(peer, (TServer)obj);
        return new object();
    }

    protected override sealed void DestroyInstance(IPeer peer, object obj)
    {
        OnServiceDestroyed(peer);
        _server = null;
    }
}
