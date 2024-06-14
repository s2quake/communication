// <copyright file="ServiceState.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

namespace JSSoft.Communication;

public enum ServiceState
{
    None,

    Opening,

    Open,

    Closing,

    Faulted,

    Closed = None,
}
