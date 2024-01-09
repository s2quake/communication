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
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JSSoft.Communication.Grpc;

sealed class AdaptorClientImpl(Channel channel, string id, IServiceHost[] serviceHosts)
    : Adaptor.AdaptorClient(channel), IPeer
{
    private static readonly TimeSpan Timeout = new(0, 0, 15);
    private readonly string _id = id;
    private Timer? _timer;

    public string Id { get; } = id;

    public Guid Token { get; private set; }

    public IServiceHost[] ServiceHosts { get; } = serviceHosts;

    public async Task OpenAsync(CancellationToken cancellationToken)
    {
        var serviceNames = ServiceHosts.Select(item => item.Name).ToArray();
        var request = new OpenRequest() { Time = DateTime.UtcNow.Ticks, Token = _id };
        request.ServiceNames.AddRange(serviceNames);
        var reply = await OpenAsync(request, cancellationToken: cancellationToken);
        Token = Guid.Parse(reply.Token);
        _timer = new Timer(Timer_TimerCallback, null, TimeSpan.Zero, Timeout);
    }

    public async Task CloseAsync(int n, CancellationToken cancellationToken)
    {
        if (_timer != null)
            await _timer.DisposeAsync();
        _timer = null;
        try
        {
            await CloseAsync(new CloseRequest() { Token = $"{Token}" }, cancellationToken: cancellationToken);
        }
        catch
        {
        }
    }

    private async void Timer_TimerCallback(object? state)
    {
        var request = new PingRequest()
        {
            Token = Token.ToString()
        };
        try
        {
            await PingAsync(request);
        }
        catch
        {
        }
    }
}
