// <copyright file="VersionCommand.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System.ComponentModel.Composition;
using JSSoft.Commands;

namespace JSSoft.Communication.Commands;

[Export(typeof(ICommand))]
[Export(typeof(VersionCommand))]
internal sealed class VersionCommand : VersionCommandBase
{
}
