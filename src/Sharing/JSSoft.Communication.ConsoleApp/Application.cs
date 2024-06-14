// <copyright file="Application.cs" company="JSSoft">
//   Copyright (c) 2024 Jeesu Choi. All Rights Reserved.
//   Licensed under the MIT License. See LICENSE.md in the project root for license information.
// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using JSSoft.Communication.Services;
using JSSoft.Terminals;

namespace JSSoft.Communication.ConsoleApp;

internal sealed class Application : IApplication, IServiceProvider
{
    private static readonly string Postfix = TerminalEnvironment.IsWindows() == true ? ">" : "$";
    private readonly ApplicationOptions _option;
    private readonly IServiceContext _serviceContext;
    private readonly CompositionContainer _container;
#if SERVER
    private readonly bool _isServer = true;
#else
    private readonly bool _isServer = false;
#endif

    private bool _isDisposed;
    private CancellationTokenSource? _cancellationTokenSource;
    private string title = string.Empty;

    static Application()
    {
        Logging.LogUtility.Logger = Logging.TraceLogger.Default;
    }

    public Application(ApplicationOptions option)
    {
        _container = new CompositionContainer(new AssemblyCatalog(typeof(Application).Assembly));
        _container.ComposeExportedValue<IApplication>(this);
        _container.ComposeExportedValue<IServiceProvider>(this);
        _container.ComposeExportedValue(this);
        _container.ComposeExportedValue(option);
        _option = option;
        _serviceContext = _container.GetExportedValue<IServiceContext>();
        _serviceContext.Opened += ServiceContext_Opened;
        _serviceContext.Closed += ServiceContext_Closed;
        Title = "Server";
        if (_container.GetExportedValue<INotifyUserService>() is { } userServiceNotification)
        {
            userServiceNotification = _container.GetExportedValue<INotifyUserService>();
            userServiceNotification.LoggedIn += UserServiceNotification_LoggedIn;
            userServiceNotification.LoggedOut += UserServiceNotification_LoggedOut;
            userServiceNotification.MessageReceived += UserServiceNotification_MessageReceived;
        }
    }

    public bool IsOpened { get; private set; }

    public string Title
    {
        get => title;
        set
        {
            title = value;
            Console.Title = value;
        }
    }

    internal Guid Token { get; set; }

    internal Guid UserToken { get; private set; }

    internal string UserId { get; private set; } = string.Empty;

    private TextWriter Out => Console.Out;

    private SystemTerminal Terminal => _container.GetExportedValue<SystemTerminal>();

    public void Dispose()
    {
        if (_isDisposed != true)
        {
            _container.Dispose();
            _isDisposed = true;
        }
    }

    public async Task StartAsync()
    {
        if (_cancellationTokenSource != null)
        {
            throw new InvalidOperationException("Application is already started.");
        }

        _cancellationTokenSource = new CancellationTokenSource();
        _serviceContext.EndPoint = new DnsEndPoint(_option.Host, _option.Port);
        try
        {
            Token = await _serviceContext.OpenAsync(_cancellationTokenSource.Token);
        }
        catch
        {
            await _serviceContext.AbortAsync();
        }

        UpdatePrompt();
        await Terminal.StartAsync(_cancellationTokenSource.Token);
    }

    public async Task StopAsync(int exitCode)
    {
        if (_cancellationTokenSource == null)
        {
            throw new InvalidOperationException("Application is not started.");
        }

        await _cancellationTokenSource.CancelAsync();
        if (_serviceContext.ServiceState == ServiceState.Open)
        {
            _serviceContext.Closed -= ServiceContext_Closed;
            try
            {
                await _serviceContext.CloseAsync(Token, cancellationToken: default);
            }
            catch
            {
                await _serviceContext.AbortAsync();
            }
        }

        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = null;
    }

    object? IServiceProvider.GetService(Type serviceType)
    {
        if (serviceType == typeof(IServiceProvider))
        {
            return this;
        }

        if (typeof(IEnumerable).IsAssignableFrom(serviceType)
            && serviceType.GenericTypeArguments.Length == 1)
        {
            var itemType = serviceType.GenericTypeArguments.First();
            var contractName = AttributedModelServices.GetContractName(itemType);
            var items = _container.GetExportedValues<object>(contractName);
            var listGenericType = typeof(List<>);
            var list = listGenericType.MakeGenericType(itemType);
            var ci = list.GetConstructor([typeof(int)])!;
            var instance = (IList)ci.Invoke([items.Count(),])!;
            foreach (var item in items)
            {
                instance.Add(item);
            }

            return instance;
        }
        else
        {
            var contractName = AttributedModelServices.GetContractName(serviceType);
            return _container.GetExportedValue<object>(contractName);
        }
    }

    internal void Login(string userId, Guid token)
    {
        UserId = userId;
        UserToken = token;
        UpdatePrompt();
        Out.WriteLine("사용자 관련 명령을 수행하려면 'help user' 을(를) 입력하세요.");
    }

    internal void Logout()
    {
        UserId = string.Empty;
        UserToken = Guid.Empty;
        UpdatePrompt();
    }

    private void UpdatePrompt()
    {
        var prompt = string.Empty;
        var isOpened = IsOpened;
        var userId = UserId;

        if (isOpened == true)
        {
            prompt = EndPointUtility.ToString(_serviceContext.EndPoint);
            if (userId != string.Empty)
            {
                prompt += $"@{userId}";
            }
        }

        Terminal.Prompt = $"{prompt} {Postfix} ";
    }

    private void ServiceContext_Opened(object? sender, EventArgs e)
    {
        IsOpened = true;
        UpdatePrompt();

        if (_isServer)
        {
            Title = $"Server {EndPointUtility.ToString(_serviceContext.EndPoint)}";
            Out.WriteLine("서버가 시작되었습니다.");
        }
        else
        {
            Title = $"Client {EndPointUtility.ToString(_serviceContext.EndPoint)}";
            Out.WriteLine("서버에 연결되었습니다.");
        }

        Out.WriteLine("사용 가능한 명령을 확인려면 '--help' 을(를) 입력하세요.");
        Out.WriteLine("로그인을 하려면 'login admin admin' 을(를) 입력하세요.");
    }

    private void ServiceContext_Closed(object? sender, EventArgs e)
    {
        IsOpened = false;
        UpdatePrompt();
        if (_isServer)
        {
            Title = $"Server - Closed";
            Out.WriteLine("서버가 중단되었습니다.");
            Out.WriteLine("서버를 시작하려면 'open' 을(를) 입력하세요.");
        }
        else
        {
            Title = $"Client - Disconnected";
            Out.WriteLine("서버와 연결이 끊어졌습니다.");
            Out.WriteLine("서버에 연결하려면 'open' 을(를) 입력하세요.");
        }
    }

    private void UserServiceNotification_LoggedIn(object? sender, UserEventArgs e)
    {
        Out.WriteLine($"User logged in: {e.UserId}");
    }

    private void UserServiceNotification_LoggedOut(object? sender, UserEventArgs e)
    {
        Out.WriteLine($"User logged out: {e.UserId}");
    }

    private void UserServiceNotification_MessageReceived(object? sender, UserMessageEventArgs e)
    {
        if (e.Sender == UserId)
        {
            var message = $"to '{e.Receiver}'에게 귓속말: {e.Message}";
            var text = TerminalStringBuilder.GetString(message, TerminalColorType.BrightMagenta);
            Out.WriteLine(text);
        }
        else if (e.Receiver == UserId)
        {
            var message = $"from '{e.Receiver}': {e.Message}";
            var text = TerminalStringBuilder.GetString(message, TerminalColorType.BrightMagenta);
            Out.WriteLine(text);
        }
    }
}
