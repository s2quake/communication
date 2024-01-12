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

using Grpc.Core;
using JSSoft.Communication.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace JSSoft.Communication.Grpc;

sealed class AdaptorServer : IAdaptor
{
    private static readonly TimeSpan PingTimeout = new(0, 0, 30);
    private static readonly string localAddress = "127.0.0.1";
    private readonly IServiceContext _serviceContext;
    private readonly IReadOnlyDictionary<string, IService> _serviceByName;
    private readonly Dictionary<IService, MethodDescriptorCollection> _methodsByService;
    private int _closeCode = int.MinValue;
    private CancellationTokenSource? _cancellationTokenSource;
    private Server? _server;
    private AdaptorServerImpl? _adaptor;
    private ISerializer? _serializer;
    private readonly Timer _timer;
    private EventHandler? _disconnectedEventHandler;

    static AdaptorServer()
    {
        var addressList = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
        var address = addressList.FirstOrDefault(item => $"{item}" != "127.0.0.1" && item.AddressFamily == AddressFamily.InterNetwork);
        if (address != null)
            localAddress = $"{address}";
    }

    public AdaptorServer(IServiceContext serviceContext, IInstanceContext instanceContext)
    {
        _serviceContext = serviceContext;
        _serviceByName = serviceContext.Services;
        _methodsByService = _serviceByName.ToDictionary(item => item.Value, item => new MethodDescriptorCollection(item.Value));
        Peers = new PeerCollection(instanceContext);
        _timer = new Timer(Timer_TimerCallback, null, TimeSpan.Zero, PingTimeout);
    }

    public PeerCollection Peers { get; }

    public Guid Id => _serviceContext.Id;

    public async Task<OpenReply> OpenAsync(OpenRequest request, ServerCallContext context, CancellationToken cancellationToken)
    {
        var id = context.Peer;
        var token = Guid.Parse(request.Token);
        var serviceNames = request.ServiceNames;
        var services = serviceNames.Select(item => _serviceByName[item]).ToArray();
        var peer = new Peer(id, services) { Token = token };
        Peers.Add(peer);
        LogUtility.Debug($"{_serviceContext} {context.Peer}, {token} Connected");
        await Task.CompletedTask;
        return new OpenReply() { Token = $"{token}" };
    }

    public async Task<CloseReply> CloseAsync(CloseRequest request, ServerCallContext context, CancellationToken cancellationToken)
    {
        var id = context.Peer;
        var token = request.Token;
        Peers.Remove(id);
        LogUtility.Debug($"{_serviceContext} {context.Peer}, {token} Disconnected");
        await Task.CompletedTask;
        return new CloseReply();
    }

    public async Task<PingReply> PingAsync(PingRequest request, ServerCallContext context)
    {
        var id = context.Peer;
        var dateTime = DateTime.UtcNow;
        if (Peers.TryGetValue(id, out var peer) == true)
        {
            peer.Ping(dateTime);
            LogUtility.Debug($"{_serviceContext} {id}, {peer.Token} Ping({dateTime})");
            return new PingReply() { Time = peer.PingTime.Ticks };
        }
        await Task.CompletedTask;
        return new PingReply() { Time = DateTime.MinValue.Ticks };
    }

    public async Task<InvokeReply> InvokeAsync(InvokeRequest request, ServerCallContext context)
    {
        if (_serializer == null)
            throw new InvalidOperationException();
        if (_serviceByName.ContainsKey(request.ServiceName) != true)
            throw new InvalidOperationException();
        var service = _serviceByName[request.ServiceName];
        var methodDescriptors = _methodsByService[service];
        if (methodDescriptors.ContainsKey(request.Name) != true)
            throw new InvalidOperationException($"method '{request.Name}' does not exists.");

        var id = context.Peer;
        if (Peers.TryGetValue(id, out var peer) == true)
        {
            var methodDescriptor = methodDescriptors[request.Name];
            var instance = peer.Services[service];
            var args = _serializer.DeserializeMany(methodDescriptor.ParameterTypes, [.. request.Data]);
            if (methodDescriptor.IsOneWay == true)
            {
                methodDescriptor.InvokeOneWay(_serviceContext, instance, args);
                var reply = new InvokeReply()
                {
                    ID = string.Empty,
                    Data = _serializer.Serialize(typeof(void), null)
                };
                LogUtility.Debug($"{context.Peer} Invoke(one way): {request.ServiceName}.{methodDescriptor.ShortName}");
                return reply;
            }
            else
            {
                var (assemblyQualifiedName, valueType, value) = await methodDescriptor.InvokeAsync(_serviceContext, instance, args);
                var reply = new InvokeReply()
                {
                    ID = $"{assemblyQualifiedName}",
                    Data = _serializer.Serialize(valueType, value)
                };
                LogUtility.Debug($"{context.Peer} Invoke: {request.ServiceName}.{methodDescriptor.ShortName}");
                return reply;
            }
        }
        return new InvokeReply();
    }

    public async Task PollAsync(IAsyncStreamReader<PollRequest> requestStream, IServerStreamWriter<PollReply> responseStream, ServerCallContext context)
    {
        var id = context.Peer;
        var peer = Peers[id];
        try
        {
            while (await requestStream.MoveNext())
            {
                if (_closeCode != int.MinValue)
                {
                    var reply = new PollReply { Code = _closeCode };
                    await responseStream.WriteAsync(reply);
                    break;
                }
                else
                {
                    var reply = peer.Collect();
                    await responseStream.WriteAsync(reply);
                }
            }
        }
        catch
        {
            Peers.Detach(id);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _timer.DisposeAsync();
    }

    private void AddCallback(InstanceBase instance, string name, Type[] types, object?[] args)
    {
        if (_serializer == null)
            throw new UnreachableException();

        var data = _serializer.SerializeMany(types, args);
        var peers = instance.Peer is not Peer peer ? Peers.ToArray().Select(item => item.Value) : new Peer[] { peer };
        var service = instance.Service;
        foreach (var item in peers)
        {
            lock (item)
            {
                if (item.PollReplyItems.ContainsKey(service) == true)
                {
                    var callbacks = item.PollReplyItems[service];
                    var pollItem = new PollReplyItem()
                    {
                        Name = name,
                        ServiceName = instance.ServiceName
                    };
                    pollItem.Data.AddRange(data);
                    callbacks.Add(pollItem);
                }
            }
        }
    }

    private void Timer_TimerCallback(object? state)
    {
        var dateTime = DateTime.UtcNow;
        var peers = Peers.ToArray();
        var query = from item in peers
                    let peer = item.Value
                    where dateTime - peer.PingTime > PingTimeout
                    select peer;
        foreach (var item in query)
        {
            Peers.Detach(item.Id);
        }
    }

    #region IAdaptor

    async Task IAdaptor.OpenAsync(DnsEndPoint endPoint, CancellationToken cancellationToken)
    {
        _adaptor = new AdaptorServerImpl(this);
        _server = new Server()
        {
            Services = { Adaptor.BindService(_adaptor) },
            Ports = { new ServerPort(endPoint.Host, endPoint.Port, ServerCredentials.Insecure) },
        };
        if (endPoint.Host == ServiceContextBase.DefaultHost)
        {
            _server.Ports.Add(new ServerPort(localAddress, endPoint.Port, ServerCredentials.Insecure));
        }
        _cancellationTokenSource = new CancellationTokenSource();
        _serializer = _serviceContext.GetService(typeof(ISerializer)) as ISerializer;
        _closeCode = int.MinValue;
        await Task.Run(_server.Start, cancellationToken);
    }

    async Task IAdaptor.CloseAsync(CancellationToken cancellationToken)
    {
        _closeCode = 0;
        _cancellationTokenSource?.Cancel();
        while (Peers.Any() == true)
        {
            await Task.Delay(1, cancellationToken);
        }
        await _server!.ShutdownAsync();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        _adaptor = null;
        _serializer = null;
        _server = null;
    }
    
    void IAdaptor.Invoke(InstanceBase instance, string name, Type[] types, object?[] args)
    {
        AddCallback(instance, name, types, args);
    }

    void IAdaptor.InvokeOneWay(InstanceBase instance, string name, Type[] types, object?[] args)
    {
        throw new NotImplementedException();
    }

    T IAdaptor.Invoke<T>(InstanceBase instance, string name, Type[] types, object?[] args)
    {
        throw new NotImplementedException();
    }

    Task IAdaptor.InvokeAsync(InstanceBase instance, string name, Type[] types, object?[] args)
    {
        throw new NotImplementedException();
    }

    Task<T> IAdaptor.InvokeAsync<T>(InstanceBase instance, string name, Type[] types, object?[] args)
    {
        throw new NotImplementedException();
    }

    event EventHandler? IAdaptor.Disconnected
    {
        add => _disconnectedEventHandler += value;
        remove => _disconnectedEventHandler -= value;
    }

    #endregion
}
