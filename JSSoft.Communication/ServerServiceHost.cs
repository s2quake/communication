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

[ServiceHost(IsServer = true)]
public class ServerServiceHost<TService, TCallback>
    : ServiceHostBase
    where TService : class
    where TCallback : class
{
    private readonly TService _service;
    private TCallback? _callback;

    public ServerServiceHost(TService service)
        : base(typeof(TService), typeof(TCallback))
    {
        _service = service;
    }

    public ServerServiceHost()
        : base(typeof(TService), typeof(TCallback))
    {
        if (typeof(TService).IsAssignableFrom(this.GetType()) == false)
            throw new InvalidOperationException();
        _service = (this as TService)!;
    }

    public TCallback Callback => _callback ?? throw new InvalidOperationException();

    protected virtual TService CreateService(IPeer peer)
    {
        return _service;
    }

    protected virtual void DestroyService(IPeer peer, TService service)
    {
    }

    private protected override object CreateInstance(IPeer peer, object obj)
    {
        _callback = (TCallback)obj;
        return CreateService(peer);
    }

    private protected override void DestroyInstance(IPeer peer, object obj)
    {
        DestroyService(peer, (TService)obj);
        _callback = null;
    }
}

[ServiceHost(IsServer = true)]
public class ServerServiceHost<TService> : ServiceHostBase
    where TService : class
{
    private readonly TService? _service;

    public ServerServiceHost(TService service)
        : base(serviceType: typeof(TService), callbackType: typeof(void))
    {
        _service = service;
    }

    public ServerServiceHost()
        : base(serviceType: typeof(TService), callbackType: typeof(void))
    {
        if (typeof(TService).IsAssignableFrom(this.GetType()) == false)
            throw new InvalidOperationException();
        _service = this as TService;
    }

    protected virtual TService CreateService(IPeer peer)
    {
        return _service!;
    }

    protected virtual void DestroyService(IPeer peer, TService service)
    {
    }

    private protected override object CreateInstance(IPeer peer, object obj)
    {
        return CreateService(peer);
    }

    private protected override void DestroyInstance(IPeer peer, object obj)
    {
        DestroyService(peer, (TService)obj);
    }
}
