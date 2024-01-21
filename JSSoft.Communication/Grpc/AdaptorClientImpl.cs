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

sealed class AdaptorClientImpl(Channel channel, Guid id, IService[] services)
    : Adaptor.AdaptorClient(channel), IPeer
{
    public string Id { get; } = $"{id}";

    public IService[] Services { get; } = services;

    public async Task OpenAsync(CancellationToken cancellationToken)
    {
        var serviceNames = Services.Select(item => item.Name).ToArray();
        var request = new OpenRequest();
        var metaData = new Metadata() { { "id", $"{Id}" } };
        await OpenAsync(request, metaData, cancellationToken: cancellationToken);
    }

    public async Task CloseAsync(CancellationToken cancellationToken)
    {
        var request = new CloseRequest();
        var metaData = new Metadata() { { "id", $"{Id}" } };
        await CloseAsync(request, metaData, cancellationToken: cancellationToken);
    }

    public async Task<bool> TryCloseAsync(CancellationToken cancellationToken)
    {
        try
        {
            var request = new CloseRequest();
            var metaData = new Metadata() { { "id", $"{Id}" } };
            await CloseAsync(request, metaData, cancellationToken: cancellationToken);
        }
        catch
        {
            return true;
        }
        return false;
    }
}
