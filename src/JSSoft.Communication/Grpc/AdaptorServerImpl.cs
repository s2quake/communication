// <copyright file="AdaptorServerImpl.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System.Threading.Tasks;
using Grpc.Core;

namespace JSSoft.Communication.Grpc;

internal sealed class AdaptorServerImpl(AdaptorServer adaptorServer) : Adaptor.AdaptorBase
{
    private readonly AdaptorServer _adaptorServer = adaptorServer;

    public override Task<OpenReply> Open(OpenRequest request, ServerCallContext context)
        => _adaptorServer.OpenAsync(request, context, context.CancellationToken);

    public override Task<CloseReply> Close(CloseRequest request, ServerCallContext context)
        => _adaptorServer.CloseAsync(request, context, context.CancellationToken);

    public override Task<PingReply> Ping(PingRequest request, ServerCallContext context)
        => _adaptorServer.PingAsync(request, context);

    public override Task<InvokeReply> Invoke(InvokeRequest request, ServerCallContext context)
        => _adaptorServer.InvokeAsync(request, context);

    public override Task Poll(
        IAsyncStreamReader<PollRequest> requestStream,
        IServerStreamWriter<PollReply> responseStream,
        ServerCallContext context)
        => _adaptorServer.PollAsync(requestStream, responseStream, context);
}
