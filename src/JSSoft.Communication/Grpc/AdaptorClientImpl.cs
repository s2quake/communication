// <copyright file="AdaptorClientImpl.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
#if NETSTANDARD
using GrpcChannel = Grpc.Core.Channel;
#elif NET
using GrpcChannel = Grpc.Net.Client.GrpcChannel;
#endif

namespace JSSoft.Communication.Grpc;

internal sealed class AdaptorClientImpl(GrpcChannel channel, Guid id, IService[] services)
    : Adaptor.AdaptorClient(channel), IPeer
{
    public string Id { get; } = $"{id}";

    public IService[] Services { get; } = services;

    public async Task OpenAsync(CancellationToken cancellationToken)
    {
        var request = new OpenRequest();
        var metaData = new Metadata() { { "id", $"{Id}" } };
        try
        {
            await OpenAsync(request, metaData, cancellationToken: cancellationToken);
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

    public async Task CloseAsync(CancellationToken cancellationToken)
    {
        var request = new CloseRequest();
        var metaData = new Metadata() { { "id", $"{Id}" } };
        try
        {
            await CloseAsync(request, metaData, cancellationToken: cancellationToken);
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
