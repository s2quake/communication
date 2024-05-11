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
using JSSoft.Communication.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace JSSoft.Communication.Grpc;

sealed class AdaptorClient : IAdaptor
{
    private static readonly TimeSpan Timeout = new(0, 0, 15);

    private readonly IServiceContext _serviceContext;
    private readonly IInstanceContext _instanceContext;
    private readonly IReadOnlyDictionary<string, IService> _serviceByName;
    private readonly Dictionary<IService, MethodDescriptorCollection> _methodsByService;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _task;
    private Channel? _channel;
    private AdaptorClientImpl? _adaptorImpl;
    private ISerializer? _serializer;
    private PeerDescriptor? _descriptor;
    private Timer? _timer;

    public AdaptorClient(IServiceContext serviceContext, IInstanceContext instanceContext)
    {
        _serviceContext = serviceContext;
        _instanceContext = instanceContext;
        _serviceByName = serviceContext.Services;
        _methodsByService = _serviceByName.ToDictionary(item => item.Value, item => new MethodDescriptorCollection(item.Value));
    }

    public async Task OpenAsync(EndPoint endPoint, CancellationToken cancellationToken)
    {
        if (_adaptorImpl != null)
            throw new InvalidOperationException("Already opened.");
        try
        {
            _channel = new Channel(EndPointUtility.ToString(endPoint), ChannelCredentials.Insecure);
            await _channel.ConnectAsync(deadline: DateTime.UtcNow.AddSeconds(15));
            _adaptorImpl = new AdaptorClientImpl(_channel, _serviceContext.Id, _serviceByName.Values.ToArray());
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
        _cancellationTokenSource?.Cancel();
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
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
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

    public event EventHandler? Disconnected;

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
        }
    }

    private async Task PollAsync(CancellationToken cancellationToken)
    {
        if (_adaptorImpl == null)
            throw new InvalidOperationException("adaptor is not set.");

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
            GrpcEnvironment.Logger.Error(e, e.Message);
        }
        if (closeCode != int.MinValue)
        {
            Disconnected?.Invoke(this, EventArgs.Empty);
        }
        _serviceContext.Debug("Poll finished.");

        static async Task<bool> MoveAsync(AsyncDuplexStreamingCall<PollRequest, PollReply> call, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested == true)
                return false;
            using var cancellationTokenSource = new CancellationTokenSource(Timeout);
            return await call.ResponseStream.MoveNext(cancellationTokenSource.Token);
        }
    }

    private void InvokeCallback(IService service, string name, string[] data)
    {
        if (_adaptorImpl == null)
            throw new InvalidOperationException("adaptor is not set.");
        var methodDescriptors = _methodsByService[service];
        if (methodDescriptors.Contains(name) != true)
            throw new InvalidOperationException("Invalid method name.");

        var methodDescriptor = methodDescriptors[name];
        var args = _serializer!.DeserializeMany(methodDescriptor.ParameterTypes, data);
        var instance = _descriptor!.ClientInstances[service];
        Task.Run(() => methodDescriptor.InvokeAsync(_serviceContext, instance, args));
    }

    private void InvokeCallback(PollReply reply)
    {
        foreach (var item in reply.Items)
        {
            var service = _serviceByName[item.ServiceName];
            InvokeCallback(service, item.Name, [.. item.Data]);
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
            throw new InvalidOperationException("serializer is not set.");

        if (Newtonsoft.Json.JsonConvert.DeserializeObject(data, exceptionType) is Exception exception)
            throw exception;

        throw new UnreachableException($"This code should not be reached in {nameof(ThrowException)}.");
    }

    #region IAdaptor

    void IAdaptor.Invoke(InstanceBase instance, string name, Type[] types, object?[] args)
    {
        if (_adaptorImpl == null)
            throw new InvalidOperationException("adaptor is not set.");

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

    async void IAdaptor.InvokeOneWay(InstanceBase instance, string name, Type[] types, object?[] args)
    {
        if (_adaptorImpl == null)
            throw new InvalidOperationException("adaptor is not set.");

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
        }
    }

    T IAdaptor.Invoke<T>(InstanceBase instance, string name, Type[] types, object?[] args)
    {
        if (_adaptorImpl == null)
            throw new InvalidOperationException("adaptor is not set.");
        if (_serializer == null)
            throw new InvalidOperationException("serializer is not set.");

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
            return value;

        throw new UnreachableException($"This code should not be reached in {nameof(IAdaptor.Invoke)}.");
    }

    async Task IAdaptor.InvokeAsync(InstanceBase instance, string name, Type[] types, object?[] args, CancellationToken cancellationToken)
    {
        if (_adaptorImpl == null)
            throw new InvalidOperationException("adaptor is not set.");
        if (_serializer == null)
            throw new InvalidOperationException("serializer is not set.");

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
            var reply = await _adaptorImpl.InvokeAsync(request, metaData, cancellationToken: cancellationToken);
            HandleReply(reply);
        }
        catch (RpcException e)
        {
            if (e.StatusCode == StatusCode.Cancelled)
                cancellationToken.ThrowIfCancellationRequested();
            throw;
        }
    }

    async Task<T> IAdaptor.InvokeAsync<T>(InstanceBase instance, string name, Type[] types, object?[] args, CancellationToken cancellationToken)
    {
        if (_adaptorImpl == null)
            throw new InvalidOperationException("adaptor is not set.");
        if (_serializer == null)
            throw new InvalidOperationException("serializer is not set.");

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
            var reply = await _adaptorImpl.InvokeAsync(request, metaData, cancellationToken: cancellationToken);
            HandleReply(reply);
            return _serializer.Deserialize(typeof(T), reply.Data) is T value ? value : default!;
        }
        catch (RpcException e)
        {
            if (e.StatusCode == StatusCode.Cancelled)
                cancellationToken.ThrowIfCancellationRequested();
            throw;
        }
    }

    #endregion
}
