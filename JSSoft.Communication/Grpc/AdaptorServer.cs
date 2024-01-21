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
using JSSoft.Communication.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace JSSoft.Communication.Grpc;

record struct CallbackData(IService Service, string Name, string[] Data);

sealed class AdaptorServer : IAdaptor
{
    private static readonly TimeSpan Timeout = new(0, 0, 30);
    private static readonly TimeSpan PollTimeout = new(0, 0, 10);
    private static readonly string localAddress = "127.0.0.1";
    private readonly IServiceContext _serviceContext;
    private readonly IReadOnlyDictionary<string, IService> _serviceByName;
    private readonly Dictionary<IService, MethodDescriptorCollection> _methodsByService;
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
        _timer = new Timer(Timer_TimerCallback, null, TimeSpan.Zero, Timeout);
    }

    public PeerCollection Peers { get; }

    public Guid Id => _serviceContext.Id;

    public async Task<OpenReply> OpenAsync(OpenRequest request, ServerCallContext context, CancellationToken cancellationToken)
    {
        var id = context.Peer;
        if (Peers.TryGetValue(id, out var peer) == true)
            throw new InvalidOperationException();

        var token = Guid.Parse(request.Token);
        var serviceNames = request.ServiceNames;
        var services = serviceNames.Select(item => _serviceByName[item]).ToArray();
        Peers.Add(_serviceContext, id, token);
        await Task.CompletedTask;
        return new OpenReply() { Token = $"{token}" };
    }

    public async Task<CloseReply> CloseAsync(CloseRequest request, ServerCallContext context, CancellationToken cancellationToken)
    {
        var id = context.Peer;
        if (Peers.TryGetValue(id, out var peer) != true)
            throw new InvalidOperationException();

        Peers.Remove(_serviceContext, id, closeCode: 0);
        await Task.CompletedTask;
        return new CloseReply();
    }

    public async Task<PingReply> PingAsync(PingRequest request, ServerCallContext context)
    {
        var id = context.Peer;
        var dateTime = DateTime.UtcNow;
        if (Peers.TryGetValue(id, out var peer) != true)
            throw new InvalidOperationException();

        peer.PingTime = dateTime;
        _serviceContext.Debug($"{id}, {peer.Token} Ping({dateTime})");
        await Task.CompletedTask;
        return new PingReply() { Time = peer.PingTime.Ticks };
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
        if (Peers.TryGetValue(id, out var peer) != true)
            throw new InvalidOperationException();

        var methodDescriptor = methodDescriptors[request.Name];
        var cancellationToken = methodDescriptor.Iscancelable == true ? (CancellationToken?)context.CancellationToken : null;
        var instance = peer.Services[service];
        var args = _serializer.DeserializeMany(methodDescriptor.ParameterTypes, [.. request.Data], cancellationToken);
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

    public async Task PollAsync(IAsyncStreamReader<PollRequest> requestStream, IServerStreamWriter<PollReply> responseStream, ServerCallContext context)
    {
        var id = context.Peer;
        if (Peers.TryGetValue(id, out var peer) != true)
            throw new InvalidOperationException();

        using var manualResetEvent = new ManualResetEvent(initialState: false);
        var cancellationToken = peer.Begin(manualResetEvent);
        try
        {
            while (await MoveAsync() == true)
            {
                var reply = peer.Collect();
                _serviceContext.Debug("write 1");
                await responseStream.WriteAsync(reply);
                _serviceContext.Debug("write 2");
                if (cancellationToken.IsCancellationRequested == true)
                    break;
                _serviceContext.Debug("wait 1");
                manualResetEvent.WaitOne(PollTimeout);
                _serviceContext.Debug("wait 2");
            }
        }
        catch (Exception e)
        {
            _serviceContext.Error(e.Message);
        }
        peer.End();
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
        await ValueTask.CompletedTask;
        GC.SuppressFinalize(this);
    }

    private void AddCallback(InstanceBase instance, string name, Type[] types, object?[] args)
    {
        if (_serializer == null)
            throw new UnreachableException();

        var data = _serializer.SerializeMany(types, args);
        var peers = instance.Peer is not Peer peer ? Peers.ToArray().Select(item => item.Value) : new Peer[] { peer };
        var service = instance.Service;
        var callbackData = new CallbackData(service, name, data);
        Parallel.ForEach(peers, item => item.Add(callbackData));
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

    #region IAdaptor

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

    void IAdaptor.Invoke(InstanceBase instance, string name, Type[] types, object?[] args)
    {
        AddCallback(instance, name, types, args);
    }

    void IAdaptor.InvokeOneWay(InstanceBase instance, string name, Type[] types, object?[] args)
    {
        AddCallback(instance, name, types, args);
    }

    T IAdaptor.Invoke<T>(InstanceBase instance, string name, Type[] types, object?[] args)
    {
        throw new NotImplementedException();
    }

    Task IAdaptor.InvokeAsync(InstanceBase instance, string name, Type[] types, object?[] args, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    Task<T> IAdaptor.InvokeAsync<T>(InstanceBase instance, string name, Type[] types, object?[] args, CancellationToken cancellationToken)
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
