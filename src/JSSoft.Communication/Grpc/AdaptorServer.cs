// <copyright file="AdaptorServer.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using JSSoft.Communication.Extensions;
using JSSoft.Communication.Logging;
#if NET
using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
#endif

namespace JSSoft.Communication.Grpc;

internal sealed class AdaptorServer : IAdaptor
{
    private static readonly TimeSpan Timeout = new(0, 0, 30);
    private static readonly TimeSpan PollTimeout = new(0, 0, 10);
    private readonly IServiceContext _serviceContext;
    private readonly IReadOnlyDictionary<string, IService> _serviceByName;
    private readonly Dictionary<IService, MethodDescriptorCollection> _methodsByService;
    private readonly Timer _timer;
#if NETSTANDARD
    private Server? _server;
#pragma warning disable S1450
    private AdaptorServerImpl? _adaptor;
#pragma warning restore S1450
#elif NET
    private IHost? _host;
#endif
    private ISerializer? _serializer;
    private EventHandler? _disconnectedEventHandler;

    public AdaptorServer(IServiceContext serviceContext, IInstanceContext instanceContext)
    {
        _serviceContext = serviceContext;
        _serviceByName = serviceContext.Services;
        _methodsByService = _serviceByName.ToDictionary(
            keySelector: item => item.Value,
            elementSelector: item => new MethodDescriptorCollection(item.Value.ServerType));
        Peers = new PeerCollection(instanceContext);
        _timer = new Timer(Timer_TimerCallback, null, TimeSpan.Zero, Timeout);
    }

    event EventHandler? IAdaptor.Disconnected
    {
        add => _disconnectedEventHandler += value;
        remove => _disconnectedEventHandler -= value;
    }

    public PeerCollection Peers { get; }

    public Guid Id => _serviceContext.Id;

    public async Task<OpenReply> OpenAsync(
        OpenRequest request, ServerCallContext context, CancellationToken cancellationToken)
    {
        var id = GetId(context);
        if (Peers.TryGetValue(id, out var peer) == true)
        {
            throw new InvalidOperationException($"The peer '{id}' already exists.");
        }

        Peers.Add(_serviceContext, id);
        await Task.Delay(1, cancellationToken);
        return new OpenReply();
    }

    public async Task<CloseReply> CloseAsync(
        CloseRequest request, ServerCallContext context, CancellationToken cancellationToken)
    {
        var id = GetId(context);
        if (Peers.TryGetValue(id, out var peer) != true)
        {
            throw new InvalidOperationException($"The peer '{id}' does not exists.");
        }

        Peers.Remove(_serviceContext, id, closeCode: 0);
        await Task.Delay(1, cancellationToken);
        return new CloseReply();
    }

    public async Task<PingReply> PingAsync(PingRequest request, ServerCallContext context)
    {
        var id = GetId(context);
        var dateTime = DateTime.UtcNow;
        if (Peers.TryGetValue(id, out var peer) != true)
        {
            throw new InvalidOperationException($"The peer '{id}' does not exists.");
        }

        peer.PingTime = dateTime;
        _serviceContext.Debug($"{id} Ping({dateTime})");
        await Task.CompletedTask;
        return new PingReply();
    }

    public async Task<InvokeReply> InvokeAsync(InvokeRequest request, ServerCallContext context)
    {
        if (_serializer == null)
        {
            throw new InvalidOperationException("Serializer is not set.");
        }

        var serviceName = request.ServiceName;
        if (_serviceByName.TryGetValue(serviceName, out var service) != true)
        {
            throw new InvalidOperationException(
                $"Service '{serviceName}' does not exists.");
        }

        var methodDescriptors = _methodsByService[service];
        if (methodDescriptors.Contains(request.Name) != true)
        {
            throw new InvalidOperationException($"Method '{request.Name}' does not exists.");
        }

        var id = GetId(context);
        if (Peers.TryGetValue(id, out var peer) != true)
        {
            throw new InvalidOperationException($"The peer '{id}' does not exists.");
        }

        var methodDescriptor = methodDescriptors[request.Name];
        var isCancelable = methodDescriptor.IsCancelable;
        var cancellationToken = isCancelable ? (CancellationToken?)context.CancellationToken : null;
        var instance = peer.Services[service];
        var args = _serializer.DeserializeMany(
            types: methodDescriptor.ParameterTypes,
            datas: [.. request.Data],
            cancellationToken: cancellationToken);
        if (methodDescriptor.IsOneWay == true)
        {
            var reply = new InvokeReply()
            {
                ID = string.Empty,
                Data = _serializer.Serialize(typeof(void), null),
            };
            var methodShortName = methodDescriptor.ShortName;

            methodDescriptor.InvokeOneWay(_serviceContext, instance, args);
            _serviceContext.Debug($"{id} Invoke(one way): {serviceName}.{methodShortName}");
            return reply;
        }
        else
        {
            var result = await methodDescriptor.InvokeAsync(_serviceContext, instance, args);
            var reply = new InvokeReply()
            {
                ID = result.AssemblyQualifiedName,
                Data = _serializer.Serialize(result.ValueType, result.Value),
            };

            _serviceContext.Debug($"{id} Invoke: {methodDescriptor.Name}");
            return reply;
        }
    }

    public async Task PollAsync(
        IAsyncStreamReader<PollRequest> requestStream,
        IServerStreamWriter<PollReply> responseStream,
        ServerCallContext context)
    {
        var id = GetId(context);
        if (Peers.TryGetValue(id, out var peer) != true)
        {
            throw new InvalidOperationException($"The peer '{id}' does not exists.");
        }

        using var manualResetEvent = new ManualResetEvent(initialState: true);
        var cancellationToken = peer.BeginPolling(manualResetEvent);
        try
        {
            while (await MoveAsync() == true)
            {
                var reply = peer.Collect();
                await responseStream.WriteAsync(reply);
                if (cancellationToken.IsCancellationRequested == true)
                {
                    break;
                }

                if (peer.CanCollect == true)
                {
                    continue;
                }

                manualResetEvent.Reset();
                manualResetEvent.WaitOne(PollTimeout);
            }
        }
        catch (Exception e)
        {
            _serviceContext.Error(e.Message);
        }

        peer.EndPolling();
        _serviceContext.Debug("Poll finished.");

        async Task<bool> MoveAsync()
        {
            using var cancellationTokenSource = new CancellationTokenSource(Timeout);
            return await requestStream.MoveNext(cancellationTokenSource.Token);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _timer.DisposeAsync();
        GC.SuppressFinalize(this);
    }

#if NETSTANDARD
    async Task IAdaptor.OpenAsync(EndPoint endPoint, CancellationToken cancellationToken)
    {
        _adaptor = new AdaptorServerImpl(this);
        _server = new Server()
        {
            Services = { Adaptor.BindService(_adaptor) },
            Ports = { EndPointUtility.GetServerPort(endPoint, ServerCredentials.Insecure) },
        };
        _serializer = _serviceContext.GetService(typeof(ISerializer)) as ISerializer;
        await Task.Run(_server.Start, cancellationToken);
    }

    async Task IAdaptor.CloseAsync(CancellationToken cancellationToken)
    {
        await Peers.DisconnectAsync(_serviceContext, cancellationToken);
        await _server!.ShutdownAsync();
        _adaptor = null;
        _serializer = null;
        _server = null;
    }

#elif NET
    async Task IAdaptor.OpenAsync(EndPoint endPoint, CancellationToken cancellationToken)
    {
        var builder = Host.CreateDefaultBuilder();
        builder.ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.ConfigureKestrel(options =>
            {
                options.Limits.MaxConcurrentConnections = 100;
                options.Limits.MaxConcurrentUpgradedConnections = 100;
                options.Limits.MaxRequestBodySize = 10 * 1024;
                options.Limits.MinRequestBodyDataRate =
                    new MinDataRate(bytesPerSecond: 100, gracePeriod: TimeSpan.FromSeconds(10));
                options.Limits.MinResponseDataRate =
                    new MinDataRate(bytesPerSecond: 100, gracePeriod: TimeSpan.FromSeconds(10));
                options.ConfigureEndpointDefaults(listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http2;
                });
                options.Listen(EndPointUtility.ConvertToIPEndPoint(endPoint));
            });
            webBuilder.Configure((context, app) =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGrpcService<AdaptorServerImpl>();
                });
            });
            webBuilder.ConfigureServices(services =>
            {
                services.AddGrpc();
                services.AddSingleton(this);
            });
        });

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
        });

        _host = builder.Build();
        _serializer = _serviceContext.GetService(typeof(ISerializer)) as ISerializer;
        await _host.StartAsync(cancellationToken);
    }

    async Task IAdaptor.CloseAsync(CancellationToken cancellationToken)
    {
        await Peers.DisconnectAsync(_serviceContext, cancellationToken);
        await _host!.StopAsync(cancellationToken);
        _serializer = null;
        _host.Dispose();
        _host = null;
    }
#endif

    void IAdaptor.Invoke(InvokeOptions options)
    {
        AddCallback(options);
    }

    void IAdaptor.InvokeOneWay(InvokeOptions options)
    {
        AddCallback(options);
    }

    T IAdaptor.Invoke<T>(InvokeOptions options)
    {
        throw new NotSupportedException(
            $"This method '{nameof(IAdaptor.Invoke)}' is not supported.");
    }

    Task IAdaptor.InvokeAsync(InvokeOptions options, CancellationToken cancellationToken)
    {
        throw new NotSupportedException(
            $"This method '{nameof(IAdaptor.InvokeAsync)}' is not supported.");
    }

    Task<T> IAdaptor.InvokeAsync<T>(InvokeOptions options, CancellationToken cancellationToken)
    {
        throw new NotSupportedException(
            $"This method '{nameof(IAdaptor.InvokeAsync)}' is not supported.");
    }

    private static string GetId(ServerCallContext context)
    {
        if (context.RequestHeaders.Get("id") is { } entry)
        {
            return entry.Value;
        }

        throw new ArgumentException("The id is not found.");
    }

    private void AddCallback(InvokeOptions options)
    {
        if (_serializer == null)
        {
            throw new UnreachableException("Serializer is not set.");
        }

        var instance = options.Instance;
        var name = options.Name;
        var types = options.Types;
        var args = options.Args;
        var data = _serializer.SerializeMany(types, args);
        var peers = instance.Peer is not Peer peer ? Peers.Select(item => item.Value) : [peer];
        var service = instance.Service;
        var callbackData = new CallbackData(service, name, data);
        Parallel.ForEach(peers, item => item.AddCallback(callbackData));
    }

    private void Timer_TimerCallback(object? state)
    {
        var dateTime = DateTime.UtcNow;
        var peers = Peers.ToArray();
        var query = from item in peers
                    let peer = item.Value
                    where dateTime - peer.PingTime > Timeout
                    select peer;
        foreach (var item in query)
        {
            Peers.Remove(_serviceContext, item.Id, closeCode: -1);
        }
    }
}
