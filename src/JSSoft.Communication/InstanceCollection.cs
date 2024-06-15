// <copyright file="InstanceCollection.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System.Collections.Generic;

namespace JSSoft.Communication;

internal sealed class InstanceCollection : Dictionary<IService, object>
{
}
