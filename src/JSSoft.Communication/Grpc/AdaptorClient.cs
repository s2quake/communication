// <copyright file="AdaptorClient.cs" company="JSSoft">
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
using Newtonsoft.Json;

#if NETSTANDARD
using GrpcChannel = Grpc.Core.Channel;
#elif NET
using System.Diagnostics;
using GrpcChannel = Grpc.Net.Client.GrpcChannel;
#endif

namespace JSSoft.Communication.Grpc;

internal sealed class AdaptorClient : IAdaptor
{
    private static readonly TimeSpan Timeout = new(0, 0, 15);

    private readonly IServiceContext _serviceContext;
    private readonly IInstanceContext _instanceContext;
    private readonly IReadOnlyDictionary<string, IService> _serviceByName;
    private readonly Dictionary<IService, MethodDescriptorCollection> _methodsByService;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _task;
    private GrpcChannel? _channel;
    private AdaptorClientImpl? _adaptorImpl;
    private ISerializer? _serializer;
    private PeerDescriptor? _descriptor;
    private Timer? _timer;

    public AdaptorClient(IServiceContext serviceContext, IInstanceContext instanceContext)
    {
        _serviceContext = serviceContext;
        _instanceContext = instanceContext;
        _serviceByName = serviceContext.Services;
        _methodsByService = _serviceByName.ToDictionary(
            keySelector: item => item.Value,
            elementSelector: item => new MethodDescriptorCollection(item.Value.ClientType));
    }

    public event EventHandler? Disconnected;

    public async Task OpenAsync(EndPoint endPoint, CancellationToken cancellationToken)
    {
        if (_adaptorImpl != null)
        {
            throw new InvalidOperationException("Already opened.");
        }

        try
        {
#if NETSTANDARD
            _channel = new Channel(EndPointUtility.ToString(endPoint), ChannelCredentials.Insecure);
            await _channel.ConnectAsync(deadline: DateTime.UtcNow.AddSeconds(15));
#elif NET
            _channel = GrpcChannel.ForAddress(
                $"http://{EndPointUtility.ConvertToIPEndPoint(endPoint)}");
            await _channel.ConnectAsync(cancellationToken);
#endif
            _adaptorImpl = new AdaptorClientImpl(
                _channel, _serviceContext.Id, _serviceByName.Values.ToArray());
            await _adaptorImpl.OpenAsync(cancellationToken);
            _descriptor = _instanceContext.CreateInstance(_adaptorImpl);
            _cancellationTokenSource = new CancellationTokenSource();
            _serializer = (ISerializer)_serviceContext.GetService(typeof(ISerializer))!;
            _task = PollAsync(_cancellationTokenSource.Token);
            _timer = new Timer(Timer_TimerCallback, null, TimeSpan.Zero, Timeout);
        }
        catch
        {
            if (_channel != null)
            {
                await _channel.ShutdownAsync();
                _channel = null;
            }

            throw;
        }
    }

    public async Task CloseAsync(CancellationToken cancellationToken)
    {
        if (_cancellationTokenSource is not null)
        {
            await _cancellationTokenSource.CancelAsync();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }

        if (_timer != null)
        {
            await _timer.DisposeAsync();
            _timer = null;
        }

        if (_adaptorImpl != null)
        {
            _instanceContext.DestroyInstance(_adaptorImpl);
            await _adaptorImpl.CloseAsync(cancellationToken);
            _adaptorImpl = null;
        }

        if (_task != null)
        {
            await _task;
        }

        if (_channel != null)
        {
            await _channel.ShutdownAsync();
            _channel = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _task = null;
        if (_timer != null)
        {
            await _timer.DisposeAsync();
            _timer = null;
        }

        if (_adaptorImpl != null)
        {
            _instanceContext.DestroyInstance(_adaptorImpl);
            await _adaptorImpl.TryCloseAsync(cancellationToken: default);
            _adaptorImpl = null;
        }

        if (_channel != null)
        {
            await _channel.ShutdownAsync();
            _channel = null;
        }

        GC.SuppressFinalize(this);
    }

    void IAdaptor.Invoke(InvokeOptions options)
    {
        if (_adaptorImpl == null)
        {
            throw new InvalidOperationException("adaptor is not set.");
        }

        var instance = options.Instance;
        var name = options.Name;
        var types = options.Types;
        var args = options.Args;
        var metaData = new Metadata { { "id", $"{_serviceContext.Id}" } };
        var data = _serializer!.SerializeMany(types, args);
        var request = new InvokeRequest
        {
            ServiceName = instance.ServiceName,
            Name = name,
            Data = { data },
        };
        var reply = _adaptorImpl.Invoke(request, metaData);
        HandleReply(reply);
    }

    async void IAdaptor.InvokeOneWay(InvokeOptions options)
    {
        if (_adaptorImpl == null)
        {
            throw new InvalidOperationException("adaptor is not set.");
        }

        var instance = options.Instance;
        var name = options.Name;
        var types = options.Types;
        var args = options.Args;
        var metaData = new Metadata { { "id", $"{_serviceContext.Id}" } };
        var data = _serializer!.SerializeMany(types, args);
        var request = new InvokeRequest
        {
            ServiceName = instance.ServiceName,
            Name = name,
            Data = { data },
        };
        try
        {
            await _adaptorImpl.InvokeAsync(request, metaData);
        }
        catch
        {
            // do nothing
        }
    }

    T IAdaptor.Invoke<T>(InvokeOptions options)
    {
        if (_adaptorImpl == null)
        {
            throw new InvalidOperationException("adaptor is not set.");
        }

        if (_serializer == null)
        {
            throw new InvalidOperationException("serializer is not set.");
        }

        var instance = options.Instance;
        var name = options.Name;
        var types = options.Types;
        var args = options.Args;
        var metaData = new Metadata { { "id", $"{_serviceContext.Id}" } };
        var data = _serializer.SerializeMany(types, args);
        var request = new InvokeRequest
        {
            ServiceName = instance.ServiceName,
            Name = name,
            Data = { data },
        };
        var reply = _adaptorImpl.Invoke(request, metaData);
        HandleReply(reply);
        if (_serializer.Deserialize(typeof(T), reply.Data) is T value)
        {
            return value;
        }

        throw new UnreachableException(
            $"This code should not be reached in {nameof(IAdaptor.Invoke)}.");
    }

    async Task IAdaptor.InvokeAsync(InvokeOptions options, CancellationToken cancellationToken)
    {
        if (_adaptorImpl == null)
        {
            throw new InvalidOperationException("adaptor is not set.");
        }

        if (_serializer == null)
        {
            throw new InvalidOperationException("serializer is not set.");
        }

        var instance = options.Instance;
        var name = options.Name;
        var types = options.Types;
        var args = options.Args;
        var metaData = new Metadata { { "id", $"{_serviceContext.Id}" } };
        var data = _serializer.SerializeMany(types, args);
        var request = new InvokeRequest
        {
            ServiceName = instance.ServiceName,
            Name = name,
            Data = { data },
        };
        try
        {
            var reply = await _adaptorImpl.InvokeAsync(
                request: request,
                headers: metaData,
                cancellationToken: cancellationToken);
            HandleReply(reply);
        }
        catch (RpcException e)
        {
            if (e.StatusCode == StatusCode.Cancelled)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            throw;
        }
    }

    async Task<T> IAdaptor.InvokeAsync<T>(
        InvokeOptions options, CancellationToken cancellationToken)
    {
        if (_adaptorImpl == null)
        {
            throw new InvalidOperationException("adaptor is not set.");
        }

        if (_serializer == null)
        {
            throw new InvalidOperationException("serializer is not set.");
        }

        var instance = options.Instance;
        var name = options.Name;
        var types = options.Types;
        var args = options.Args;
        var metaData = new Metadata { { "id", $"{_serviceContext.Id}" } };
        var data = _serializer.SerializeMany(types, args);
        var request = new InvokeRequest
        {
            ServiceName = instance.ServiceName,
            Name = name,
        };
        request.Data.AddRange(data);
        try
        {
            var reply = await _adaptorImpl.InvokeAsync(
                request: request,
                headers: metaData,
                cancellationToken: cancellationToken);
            HandleReply(reply);
            return _serializer.Deserialize(typeof(T), reply.Data) is T value ? value : default!;
        }
        catch (RpcException e)
        {
            if (e.StatusCode == StatusCode.Cancelled)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            throw;
        }
    }

    private static async Task<bool> MoveAsync(
        AsyncDuplexStreamingCall<PollRequest, PollReply> call, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested == true)
        {
            return false;
        }

        using var cancellationTokenSource = new CancellationTokenSource(Timeout);
        return await call.ResponseStream.MoveNext(cancellationTokenSource.Token);
    }

    private async void Timer_TimerCallback(object? state)
    {
        try
        {
            if (_adaptorImpl != null)
            {
                var metaData = new Metadata { { "id", $"{_serviceContext.Id}" } };
                var request = new PingRequest();
                await _adaptorImpl.PingAsync(request, metaData);
            }
        }
        catch
        {
            // do nothing
        }
    }

    private async Task PollAsync(CancellationToken cancellationToken)
    {
        if (_adaptorImpl == null)
        {
            throw new InvalidOperationException("adaptor is not set.");
        }

        var closeCode = int.MinValue;
        try
        {
            var metaData = new Metadata { { "id", $"{_serviceContext.Id}" } };
            using var call = _adaptorImpl.Poll(metaData);
            var request = new PollRequest();
            await call.RequestStream.WriteAsync(request);
            while (await MoveAsync(call, cancellationToken) == true)
            {
                var reply = call.ResponseStream.Current;
                if (reply.Code != int.MinValue)
                {
                    closeCode = reply.Code;
                    break;
                }

                InvokeCallback(reply);
                await call.RequestStream.WriteAsync(request);
            }

            await call.RequestStream.CompleteAsync();
        }
        catch (Exception e)
        {
            closeCode = -2;
            LogUtility.Error(e);
        }

        if (closeCode != int.MinValue)
        {
            Disconnected?.Invoke(this, EventArgs.Empty);
        }

        _serviceContext.Debug("Poll finished.");
    }

    private void InvokeCallback(IService service, string name, string[] data)
    {
        if (_adaptorImpl == null)
        {
            throw new InvalidOperationException("adaptor is not set.");
        }

        var methodDescriptors = _methodsByService[service];
        if (methodDescriptors.Contains(name) == true)
        {
            var methodDescriptor = methodDescriptors[name];
            var args = _serializer!.DeserializeMany(methodDescriptor.ParameterTypes, data);
            var instance = _descriptor!.ClientInstances[service];
            Task.Run(() => methodDescriptor.InvokeAsync(_serviceContext, instance, args));
        }
        else
        {
            LogUtility.Warn($"Method '{name}' is not found.");
        }
    }

    private void InvokeCallback(PollReply reply)
    {
        foreach (var item in reply.Items)
        {
            if (_serviceByName.TryGetValue(item.ServiceName, out var service) == true)
            {
                InvokeCallback(service, item.Name, [.. item.Data]);
            }
            else
            {
                LogUtility.Warn($"Service '{item.ServiceName}' is not found.");
            }
        }

        reply.Items.Clear();
    }

    private void HandleReply(InvokeReply reply)
    {
        if (reply.ID != string.Empty && Type.GetType(reply.ID) is { } exceptionType)
        {
            ThrowException(exceptionType, reply.Data);
        }
    }

    private void ThrowException(Type exceptionType, string data)
    {
        if (_serializer == null)
        {
            throw new InvalidOperationException("serializer is not set.");
        }

        if (JsonConvert.DeserializeObject(data, exceptionType) is Exception exception)
        {
            throw exception;
        }

        throw new UnreachableException(
            $"This code should not be reached in {nameof(ThrowException)}.");
    }
}
