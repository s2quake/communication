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

using JSSoft.Communication.Logging;
using JSSoft.Communication.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace JSSoft.Communication;

public abstract class ServiceContextBase : IServiceContext
{
    public const string DefaultHost = "localhost";
    public const int DefaultPort = 4004;
    private readonly ServiceInstanceBuilder? _instanceBuilder;
    private readonly InstanceContext _instanceContext;
    private readonly bool _isServer;
    private readonly string _t;
    private ISerializer? _serializer;
    private IAdaptorHost? _adaptorHost;
    private string _host = DefaultHost;
    private int _port = DefaultPort;
    private ServiceToken? _token;
    private Dispatcher? _dispatcher;
    private ServiceState _serviceState;

    protected ServiceContextBase(IServiceHost[] serviceHost)
    {
        ServiceHosts = new ServiceHostCollection(serviceHost);
        _isServer = IsServer(this);
        _t = _isServer ? "👨" : "👩";
        _instanceBuilder = ServiceInstanceBuilder.Create();
        _instanceContext = new InstanceContext(this);
    }

    public abstract IAdaptorHostProvider AdaptorHostProvider { get; }

    public abstract ISerializerProvider SerializerProvider { get; }

    public string AdaptorHostType { get; set; } = Communication.AdaptorHostProvider.DefaultName;

    public string SerializerType { get; set; } = JsonSerializerProvider.DefaultName;

    public ServiceHostCollection ServiceHosts { get; }

    public ServiceState ServiceState
    {
        get => _serviceState;
        private set
        {
            if (_serviceState != value)
            {
                Debug($"{this} {nameof(ServiceState)}.{_serviceState} => {nameof(ServiceState)}.{value}");
                _serviceState = value;
            }
        }
    }

    public string Host
    {
        get => _host;
        set
        {
            if (ServiceState != ServiceState.None)
                throw new InvalidOperationException($"cannot set host. service state is '{ServiceState}'.");
            _host = value;
        }
    }

    public int Port
    {
        get => _port;
        set
        {
            if (ServiceState != ServiceState.None)
                throw new InvalidOperationException($"cannot set port. service state is '{ServiceState}'.");
            _port = value;
        }
    }

    public Guid Id { get; } = Guid.NewGuid();

    public Dispatcher Dispatcher
    {
        get
        {
            if (_dispatcher == null)
                throw new InvalidOperationException();
            return _dispatcher;
        }
    }

    public override string ToString()
    {
        return $"{_t}[{Id}]";
    }

    public async Task<Guid> OpenAsync(CancellationToken cancellationToken)
    {
        if (ServiceState != ServiceState.None)
            throw new InvalidOperationException();
        try
        {
            ServiceState = ServiceState.Opening;
            _dispatcher = new Dispatcher(this);
            _token = ServiceToken.NewToken();
            _serializer = SerializerProvider.Create(this);
            Debug($"{this} {SerializerProvider.Name} Serializer created.");
            _adaptorHost = AdaptorHostProvider.Create(this, _instanceContext, _token);
            Debug($"{this} {AdaptorHostProvider.Name} Adaptor created.");
            _adaptorHost.Disconnected += AdaptorHost_Disconnected;
            foreach (var item in ServiceHosts.Values)
            {
                await item.OpenAsync(_token, cancellationToken);
                Debug($"{this} {item.Name} Service opened.");
            }
            _instanceContext.InitializeInstance();
            await _adaptorHost.OpenAsync(Host, Port, cancellationToken);
            Debug($"{this} {AdaptorHostProvider.Name} Adaptor opened.");
            Debug($"{this} Service Context opened.");
            ServiceState = ServiceState.Open;
            OnOpened(EventArgs.Empty);
            return _token.Guid;
        }
        catch
        {
            ServiceState = ServiceState.Faulted;
            OnFaulted(EventArgs.Empty);
            throw;
        }
    }

    public async Task CloseAsync(Guid token, int closeCode, CancellationToken cancellationToken)
    {
        if (ServiceState != ServiceState.Open)
            throw new InvalidOperationException();
        if (token == Guid.Empty || _token!.Guid != token)
            throw new ArgumentException($"invalid token: {token}", nameof(token));
        if (closeCode == int.MinValue)
            throw new ArgumentException($"invalid close code: '{closeCode}'", nameof(closeCode));
        try
        {
            ServiceState = ServiceState.Closing;
            await _adaptorHost!.CloseAsync(closeCode, cancellationToken);
            Debug($"{this} {AdaptorHostProvider!.Name} Adaptor closed.");
            _instanceContext.ReleaseInstance();
            foreach (var item in ServiceHosts.Values.Reverse())
            {
                await item.CloseAsync(_token, cancellationToken);
                Debug($"{this} {item.Name} Service closed.");
            }
            _adaptorHost.Disconnected -= AdaptorHost_Disconnected;
            await _adaptorHost.DisposeAsync();
            Debug($"{this} {AdaptorHostProvider.Name} Adaptor disposed.");
            _adaptorHost = null;
            _serializer = null;
            _dispatcher?.Dispose();
            _dispatcher = null;
            _token = ServiceToken.Empty;
            ServiceState = ServiceState.None;
            OnClosed(new CloseEventArgs(closeCode));
            Debug($"{this} Service Context closed.");
        }
        catch
        {
            ServiceState = ServiceState.Faulted;
            OnFaulted(EventArgs.Empty);
            throw;
        }
    }

    public async Task AbortAsync(Guid token)
    {
        if (ServiceState != ServiceState.Faulted)
            throw new InvalidOperationException();
        if (token == Guid.Empty || _token!.Guid != token)
            throw new ArgumentException($"invalid token: {token}", nameof(token));

        foreach (var item in ServiceHosts)
        {
            if (item.Value.ServiceState == ServiceState.Faulted)
                await item.Value.AbortAsync(_token!);
        }
        _token = null;
        _serializer = null;
        _adaptorHost = null;
        if (_adaptorHost != null)
            await _adaptorHost.DisposeAsync();
        _dispatcher?.Dispose();
        _dispatcher = null;
        ServiceState = ServiceState.None;
        OnAborted(EventArgs.Empty);
    }

    public virtual object? GetService(Type serviceType)
    {
        if (serviceType == typeof(ISerializer))
            return _serializer;
        if (_instanceContext.GetService(serviceType) is { } instanceService)
            return instanceService;
        return null;
    }

    public event EventHandler? Opened;

    public event EventHandler<CloseEventArgs>? Closed;

    public event EventHandler? Faulted;

    public event EventHandler? Aborted;

    protected virtual InstanceBase CreateInstance(Type type)
    {
        if (_instanceBuilder == null)
            throw new InvalidOperationException($"cannot create instance of {type}");
        if (type == typeof(void))
            return InstanceBase.Empty;
        var typeName = $"{type.Name}Impl";
        var instanceType = _instanceBuilder.CreateType(typeName, typeof(InstanceBase), type);
        return (InstanceBase)Activator.CreateInstance(instanceType)!;
    }

    protected virtual void OnOpened(EventArgs e)
    {
        Opened?.Invoke(this, e);
    }

    protected virtual void OnClosed(CloseEventArgs e)
    {
        Closed?.Invoke(this, e);
    }

    protected virtual void OnFaulted(EventArgs e)
    {
        Faulted?.Invoke(this, e);
    }

    protected virtual void OnAborted(EventArgs e)
    {
        Aborted?.Invoke(this, e);
    }

    internal static bool IsServer(ServiceContextBase serviceContext)
    {
        if (serviceContext.GetType().GetCustomAttribute(typeof(ServiceContextAttribute)) is ServiceContextAttribute attribute)
        {
            return attribute.IsServer;
        }
        return false;
    }

    internal static Type GetInstanceType(ServiceContextBase serviceContext, IServiceHost serviceHost)
    {
        var isServer = IsServer(serviceContext);
        if (isServer == true)
        {
            return serviceHost.CallbackType;
        }
        return serviceHost.ServiceType;
    }

    internal static bool IsPerPeer(ServiceContextBase serviceContext, IServiceHost serviceHost)
    {
        if (IsServer(serviceContext) != true)
            return false;
        var serviceType = serviceHost.ServiceType;
        if (serviceType.GetCustomAttribute(typeof(ServiceContractAttribute)) is ServiceContractAttribute attribute)
        {
            return attribute.PerPeer;
        }
        return false;
    }

    internal (object, object) CreateInstance(IServiceHost serviceHost, IPeer peer)
    {
        var adaptorHost = _adaptorHost!;
        var baseType = GetInstanceType(this, serviceHost);
        var instance = CreateInstance(baseType);
        {
            instance.ServiceHost = serviceHost;
            instance.AdaptorHost = adaptorHost;
            instance.Peer = peer;
        }
        var token = _token!;

        var impl = serviceHost.CreateInstance(token, peer, instance);
        var service = _isServer ? impl : instance;
        var callback = _isServer ? instance : impl;
        return (service, callback);
    }

    internal void DestroyInstance(IServiceHost serviceHost, IPeer peer, object service, object callback)
    {
        var token = _token!;
        if (_isServer == true)
        {
            serviceHost.DestroyInstance(token, peer, service);
        }
        else
        {
            serviceHost.DestroyInstance(token, peer, callback);
        }
    }

    private static void Debug(string message)
    {
        LogUtility.Debug(message);
    }

    private async void AdaptorHost_Disconnected(object? sender, CloseEventArgs e)
    {
        var closeCode = e.CloseCode;
        var cancellationToken = CancellationToken.None;
        ServiceState = ServiceState.Closing;
        Debug($"{this} {AdaptorHostProvider!.Name} Adaptor closed.");
        _instanceContext.ReleaseInstance();
        foreach (var item in ServiceHosts.Values.Reverse())
        {
            await item.CloseAsync(_token!, cancellationToken);
            Debug($"{this} {item.Name} Service closed.");
        }
        if (_adaptorHost != null)
        {
            await _adaptorHost.DisposeAsync();
            Debug($"{this} {AdaptorHostProvider.Name} Adaptor disposed.");
        }
        _adaptorHost = null;
        _serializer = null;
        _dispatcher?.Dispose();
        _dispatcher = null;
        _token = ServiceToken.Empty;
        ServiceState = ServiceState.None;
        OnClosed(new CloseEventArgs(closeCode));
        Debug($"{this} Service Context closed: ({closeCode}).");
    }

    #region IServiceHost

    IReadOnlyDictionary<string, IServiceHost> IServiceContext.ServiceHosts => ServiceHosts;

    #endregion
}
