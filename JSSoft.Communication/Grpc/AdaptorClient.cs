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
using System.Reflection;
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
    private string _token = string.Empty;
    private DateTime _pongDateTime;

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
            throw new InvalidOperationException();
        try
        {
            _channel = new Channel(EndPointUtility.GetString(endPoint), ChannelCredentials.Insecure);
            _adaptorImpl = new AdaptorClientImpl(_channel, $"{_serviceContext.Id}", _serviceByName.Values.ToArray());
            _token = await _adaptorImpl.OpenAsync(cancellationToken);
            _descriptor = _instanceContext.CreateInstance(_adaptorImpl);
            _cancellationTokenSource = new CancellationTokenSource();
            _serializer = (ISerializer)_serviceContext.GetService(typeof(ISerializer))!;
            _task = PollAsync(_cancellationTokenSource.Token);
            _timer = new Timer(Timer_TimerCallback, null, TimeSpan.Zero, Timeout);
            Pong();
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
            await _adaptorImpl.CloseAsync(_token, cancellationToken);
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
        _token = string.Empty;
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
            await _adaptorImpl.TryCloseAsync(_token, cancellationToken: default);
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
                var request = new PingRequest() { Token = _token };
                await _adaptorImpl.PingAsync(request);
            }
        }
        catch
        {
        }
    }

    private async Task PollAsync(CancellationToken cancellationToken)
    {
        if (_adaptorImpl == null)
            throw new InvalidOperationException();

        var closeCode = int.MinValue;
        try
        {
            using var call = _adaptorImpl.Poll();
            var request = new PollRequest { Token = _token };
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
                _serviceContext.Debug("write 1");
                await call.RequestStream.WriteAsync(request);
                _serviceContext.Debug("write 2");
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
            throw new InvalidOperationException();
        var methodDescriptors = _methodsByService[service];
        if (methodDescriptors.ContainsKey(name) != true)
            throw new InvalidOperationException();

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
        Pong();
        if (reply.ID != string.Empty && Type.GetType(reply.ID) is { } exceptionType)
        {
            ThrowException(exceptionType, reply.Data);
        }
    }

    private void ThrowException(Type exceptionType, string data)
    {
        if (_serializer == null)
            throw new InvalidOperationException();

        if (Newtonsoft.Json.JsonConvert.DeserializeObject(data, exceptionType) is Exception exception)
            throw exception;
        throw new UnreachableException();
    }

    private void Pong()
    {
        _pongDateTime = DateTime.Now;
    }

    #region IAdaptor

    void IAdaptor.Invoke(InstanceBase instance, string name, Type[] types, object?[] args)
    {
        if (_adaptorImpl == null)
            throw new InvalidOperationException();

        var token = _token;
        var data = _serializer!.SerializeMany(types, args);
        var request = new InvokeRequest
        {
            ServiceName = instance.ServiceName,
            Name = name,
            Token = token,
            Data = { data },
        };
        var reply = _adaptorImpl.Invoke(request);
        HandleReply(reply);
    }

    async void IAdaptor.InvokeOneWay(InstanceBase instance, string name, Type[] types, object?[] args)
    {
        if (_adaptorImpl == null)
            throw new InvalidOperationException();

        var token = _token;
        var data = _serializer!.SerializeMany(types, args);
        var request = new InvokeRequest
        {
            ServiceName = instance.ServiceName,
            Name = name,
            Token = token,
            Data = { data },
        };
        try
        {
            await _adaptorImpl.InvokeAsync(request);
            Pong();
        }
        catch
        {
        }
    }

    T IAdaptor.Invoke<T>(InstanceBase instance, string name, Type[] types, object?[] args)
    {
        if (_adaptorImpl == null || _serializer == null)
            throw new InvalidOperationException();

        var token = _token;
        var data = _serializer.SerializeMany(types, args);
        var request = new InvokeRequest
        {
            ServiceName = instance.ServiceName,
            Name = name,
            Token = token,
            Data = { data },
        };
        var reply = _adaptorImpl.Invoke(request);
        HandleReply(reply);
        if (_serializer.Deserialize(typeof(T), reply.Data) is T value)
            return value;
        throw new UnreachableException();
    }

    async Task IAdaptor.InvokeAsync(InstanceBase instance, string name, Type[] types, object?[] args, CancellationToken cancellationToken)
    {
        if (_adaptorImpl == null || _serializer == null)
            throw new InvalidOperationException();

        var token = _token;
        var data = _serializer.SerializeMany(types, args);
        var request = new InvokeRequest
        {
            ServiceName = instance.ServiceName,
            Name = name,
            Token = token,
            Data = { data },
        };
        try
        {
            var reply = await _adaptorImpl.InvokeAsync(request, cancellationToken: cancellationToken);
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
        if (_adaptorImpl == null || _serializer == null)
            throw new InvalidOperationException();

        var token = _token;
        var data = _serializer.SerializeMany(types, args);
        var request = new InvokeRequest
        {
            ServiceName = instance.ServiceName,
            Name = name,
            Token = token
        };
        request.Data.AddRange(data);
        try
        {
            var reply = await _adaptorImpl.InvokeAsync(request, cancellationToken: cancellationToken);
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
