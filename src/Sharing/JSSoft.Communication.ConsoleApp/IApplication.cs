// <copyright file="IApplication.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;
using System.Threading.Tasks;

namespace JSSoft.Communication.ConsoleApp;

public interface IApplication : IDisposable
{
    string Title { get; set; }

    Task StartAsync();

    Task StopAsync(int exitCode);
}
