// <copyright file="ServiceToken.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;

namespace JSSoft.Communication;

public sealed class ServiceToken
{
    internal static readonly ServiceToken Empty = new(Guid.Empty);

    internal ServiceToken(Guid guid) => Guid = guid;

    internal Guid Guid { get; }

    internal static ServiceToken NewToken() => new(Guid.NewGuid());
}
