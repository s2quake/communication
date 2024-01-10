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

[ServiceHost(IsServer = false)]
public class ClientServiceHost<TService, TCallback>
    : ServiceHostBase
    where TService : class
    where TCallback : class
{
    private readonly TCallback _callback;
    private TService? _service;

    public ClientServiceHost(TCallback callback)
        : base(typeof(TService), typeof(TCallback))
    {
        _callback = callback;
    }

    public ClientServiceHost()
        : base(typeof(TService), typeof(TCallback))
    {
        if (typeof(TService).IsAssignableFrom(this.GetType()) == false)
            throw new InvalidOperationException();
        _callback = (this as TCallback)!;
    }

    protected TService Service => _service ?? throw new InvalidOperationException();

    protected virtual TCallback CreateCallback(IPeer peer, TService service)
    {
        return _callback;
    }

    protected virtual void DestroyCallback(IPeer peer, TCallback callback)
    {
    }

    protected override sealed object CreateInstance(IPeer peer, object obj)
    {
        _service = (TService?)obj;
        return CreateCallback(peer, (TService)obj);
    }

    protected override sealed void DestroyInstance(IPeer peer, object obj)
    {
        DestroyCallback(peer, (TCallback)obj);
        _service = null;
    }
}

[ServiceHost(IsServer = false)]
public class ClientServiceHost<TService>
    : ServiceHostBase
    where TService : class
{
    private TService? _service;

    public ClientServiceHost()
        : base(serviceType: typeof(TService), callbackType: typeof(void))
    {
    }

    protected TService Service => _service ?? throw new InvalidOperationException();

    protected virtual void OnServiceCreated(IPeer peer, TService service)
    {
    }

    protected virtual void OnServiceDestroyed(IPeer peer)
    {
    }

    protected override sealed object CreateInstance(IPeer peer, object obj)
    {
        _service = (TService)obj;
        OnServiceCreated(peer, (TService)obj);
        return new object();
    }

    protected override sealed void DestroyInstance(IPeer peer, object obj)
    {
        OnServiceDestroyed(peer);
        _service = null;
    }
}
