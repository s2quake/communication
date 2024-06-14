// <copyright file="IAdaptorProvider.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

namespace JSSoft.Communication;

public interface IAdaptorProvider
{
    string Name { get; }

    IAdaptor Create(
        IServiceContext serviceContext,
        IInstanceContext instanceContext,
        ServiceToken serviceToken);
}
