// <copyright file="DefaultSerializerProvider.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

namespace JSSoft.Communication;

internal sealed class DefaultSerializerProvider : ISerializerProvider
{
    public const string DefaultName = "json";

    public static readonly DefaultSerializerProvider Default = new();

    public string Name => DefaultName;

    public ISerializer Create(IServiceContext serviceContext)
        => new DefaultSerializer();
}
