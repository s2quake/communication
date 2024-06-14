// <copyright file="AdaptorProvider.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;

namespace JSSoft.Communication;

internal sealed class AdaptorProvider : IAdaptorProvider
{
    public const string DefaultName = "grpc";
    public static readonly AdaptorProvider Default = new();

    public string Name => DefaultName;

    public IAdaptor Create(
        IServiceContext serviceContext, IInstanceContext instanceContext, ServiceToken serviceToken)
    {
        if (serviceContext is ServerContext serverContext)
        {
            return new Grpc.AdaptorServer(serverContext, instanceContext);
        }
        else if (serviceContext is ClientContext clientContext)
        {
            return new Grpc.AdaptorClient(clientContext, instanceContext);
        }

        throw new NotSupportedException($"'{serviceContext.GetType()}' is not supported.");
    }
}
