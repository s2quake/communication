// <copyright file="ServiceContextBase.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using JSSoft.Communication.Logging;

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
    private IAdaptor? _adaptor;
    private EndPoint _endPoint = new DnsEndPoint(DefaultHost, DefaultPort);
    private ServiceToken? _token;
    private ServiceState _serviceState;

    protected ServiceContextBase(IService[] services)
    {
        Services = new ServiceCollection(services);
        _isServer = IsServer(this);
        _t = _isServer ? "👨" : "👩";
        _instanceBuilder = ServiceInstanceBuilder.Create();
        _instanceContext = new InstanceContext(this);
    }

    public event EventHandler? Opened;

    public event EventHandler? Closed;

    public event EventHandler? Faulted;

    /// <summary>
    /// Disconnected event is not used on the server side.
    /// </summary>
    public event EventHandler? Disconnected;

    public event EventHandler? ServiceStateChanged;

    public virtual IAdaptorProvider AdaptorProvider => Communication.AdaptorProvider.Default;

    public virtual ISerializerProvider SerializerProvider => JsonSerializerProvider.Default;

    public ServiceCollection Services { get; }

    public ServiceState ServiceState
    {
        get => _serviceState;
        private set
        {
            if (_serviceState != value)
            {
                var serviceState = _serviceState;
                _serviceState = value;
                Debug($"{nameof(ServiceState)}.{serviceState} => {nameof(ServiceState)}.{value}");
                OnServiceStateChanged(EventArgs.Empty);
            }
        }
    }

    public EndPoint EndPoint
    {
        get => _endPoint;
        set
        {
            if (ServiceState != ServiceState.None)
            {
                var message = $"""
                    Cannot set '{nameof(EndPoint)}'. service state is '{ServiceState.None}'.
                    """;
                throw new InvalidOperationException(message);
            }

            _endPoint = value;
        }
    }

    public Guid Id { get; } = Guid.NewGuid();

    IReadOnlyDictionary<string, IService> IServiceContext.Services => Services;

    public override string ToString() => $"{_t}[{Id}]";

    public async Task<Guid> OpenAsync(CancellationToken cancellationToken)
    {
        if (ServiceState != ServiceState.None)
        {
            var message = $"Service can only be open if the state is '{ServiceState.None}'.";
            throw new InvalidOperationException(message);
        }

        try
        {
            ServiceState = ServiceState.Opening;
            _token = ServiceToken.NewToken();
            _serializer = SerializerProvider.Create(this);
            Debug($"{nameof(ISerializer)} ({SerializerProvider.Name}) Serializer created.");
            _adaptor = AdaptorProvider.Create(this, _instanceContext, _token);
            Debug($"{nameof(IAdaptor)} ({AdaptorProvider.Name}) created.");
            _instanceContext.InitializeInstance();
            Debug($"{nameof(InstanceContext)} initialized.");
            await _adaptor.OpenAsync(_endPoint, cancellationToken);
            Debug($"{nameof(IAdaptor)} ({AdaptorProvider.Name}) Adaptor opened.");
            _adaptor.Disconnected += Adaptor_Disconnected;
            Debug($"{nameof(IServiceContext)} ({this.GetType()}) opened.");
            await Task.Delay(1, cancellationToken);
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

    public async Task CloseAsync(Guid token, CancellationToken cancellationToken)
    {
        if (ServiceState != ServiceState.Open)
        {
            throw new InvalidOperationException(
                $"Service can only be closed if the state is '{ServiceState.Open}'.");
        }

        if (token == Guid.Empty || _token!.Guid != token)
        {
            throw new ArgumentException($"'{token}' is an invalid token.", nameof(token));
        }

        try
        {
            ServiceState = ServiceState.Closing;
            _adaptor!.Disconnected -= Adaptor_Disconnected;
            await _adaptor!.CloseAsync(cancellationToken);
            Debug($"{nameof(IAdaptor)} ({AdaptorProvider.Name}) closed.");
            _instanceContext.ReleaseInstance();
            Debug($"{nameof(InstanceContext)} released.");
            await _adaptor.DisposeAsync();
            Debug($"{nameof(IAdaptor)} ({AdaptorProvider.Name}) disposed.");
            _adaptor = null;
            _serializer = null;
            _token = ServiceToken.Empty;
            await Task.Delay(1, cancellationToken);
            ServiceState = ServiceState.None;
            Debug($"{nameof(IServiceContext)}({this.GetType()}) closed.");
            OnClosed(EventArgs.Empty);
        }
        catch (Exception e)
        {
            ServiceState = ServiceState.Faulted;
            OnFaulted(EventArgs.Empty);
            LogUtility.Error(e);
            throw;
        }
    }

    public async Task AbortAsync()
    {
        if (ServiceState != ServiceState.Faulted)
        {
            throw new InvalidOperationException(
                $"Service can only be aborted if the state is '{ServiceState.Faulted}'.");
        }

        _token = null;
        _serializer = null;
        _instanceContext.ReleaseInstance();
        Debug($"{nameof(InstanceContext)} released.");
        if (_adaptor is not null)
        {
            await _adaptor.DisposeAsync();
            Debug($"{nameof(IAdaptor)} ({AdaptorProvider.Name}) disposed.");
            _adaptor = null;
        }

        ServiceState = ServiceState.None;
        OnClosed(EventArgs.Empty);
    }

    public virtual object? GetService(Type serviceType)
    {
        if (serviceType == typeof(ISerializer))
        {
            return _serializer;
        }

        if (_instanceContext.GetService(serviceType) is { } instanceService)
        {
            return instanceService;
        }

        return null;
    }

    internal static bool IsServer(ServiceContextBase serviceContext)
    {
        var serviceContextType = serviceContext.GetType();
        var attribute = serviceContextType.GetCustomAttribute(typeof(ServiceContextAttribute));
        if (attribute is ServiceContextAttribute serviceContextAttribute)
        {
            return serviceContextAttribute.IsServer;
        }

        return false;
    }

    internal static Type GetInstanceType(ServiceContextBase serviceContext, IService service)
    {
        var isServer = IsServer(serviceContext);
        if (isServer == true)
        {
            return service.ClientType;
        }

        return service.ServerType;
    }

    internal static bool IsPerPeer(ServiceContextBase serviceContext, IService service)
    {
        if (IsServer(serviceContext) != true)
        {
            return false;
        }

        var serviceType = service.ServerType;
        var attribute = serviceType.GetCustomAttribute(typeof(ServiceContractAttribute));
        if (attribute is ServiceContractAttribute serviceContractAttribute)
        {
            return serviceContractAttribute.PerPeer;
        }

        return false;
    }

    internal (object ServiceInstance, object ClientInstance) CreateInstance(
        IService service, IPeer peer)
    {
        var adaptor = _adaptor!;
        var baseType = GetInstanceType(this, service);
        var instance = CreateInstance(baseType);

        instance.Service = service;
        instance.Adaptor = adaptor;
        instance.Peer = peer;

        var impl = service.CreateInstance(peer, instance);
        var serverInstance = _isServer ? impl : instance;
        var clientInstance = _isServer ? instance : impl;
        return (serverInstance, clientInstance);
    }

    internal void DestroyInstance(
        IService service, IPeer peer, object serverInstance, object clientInstance)
    {
        if (_isServer == true)
        {
            service.DestroyInstance(peer, serverInstance);
        }
        else
        {
            service.DestroyInstance(peer, clientInstance);
        }
    }

    protected virtual InstanceBase CreateInstance(Type type)
    {
        if (_instanceBuilder == null)
        {
            throw new InvalidOperationException($"Cannot create instance of {type}");
        }

        if (type == typeof(void))
        {
            return InstanceBase.Empty;
        }

        var typeName = $"{type.Name}Impl";
        var instanceType = _instanceBuilder.CreateType(typeName, typeof(InstanceBase), type);
        return (InstanceBase)Activator.CreateInstance(instanceType)!;
    }

    protected virtual void OnOpened(EventArgs e)
        => Opened?.Invoke(this, e);

    protected virtual void OnClosed(EventArgs e)
        => Closed?.Invoke(this, e);

    protected virtual void OnFaulted(EventArgs e)
        => Faulted?.Invoke(this, e);

    protected virtual void OnDisconnected(EventArgs e)
        => Disconnected?.Invoke(this, e);

    protected virtual void OnServiceStateChanged(EventArgs e)
        => ServiceStateChanged?.Invoke(this, e);

    private void Debug(string message)
        => LogUtility.Debug($"{this} {message}");

    private void Adaptor_Disconnected(object? sender, EventArgs e)
    {
        ServiceState = ServiceState.Closing;
        Debug($"{nameof(IServiceContext)} ({this.GetType()}) disconnected.");
        OnDisconnected(EventArgs.Empty);
        _instanceContext.ReleaseInstance();
        Debug($"{nameof(InstanceContext)} released.");
        _adaptor!.DisposeAsync();
        Debug($"{AdaptorProvider.Name} Adaptor disposed.");
        _adaptor = null;
        _serializer = null;
        _token = ServiceToken.Empty;
        ServiceState = ServiceState.None;
        Debug($"{nameof(IServiceContext)} ({this.GetType()}) closed.");
        OnClosed(EventArgs.Empty);
    }
}
