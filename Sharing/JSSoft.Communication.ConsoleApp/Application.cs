// MIT License
// 
// Copyright (c) 2024 Jeesu Choi
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using JSSoft.Communication.Services;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using JSSoft.Terminals;
using System.Net;

namespace JSSoft.Communication.ConsoleApp;

sealed class Application : IApplication, IServiceProvider
{
    private static readonly string postfix = TerminalEnvironment.IsWindows() == true ? ">" : "$";
    private readonly ApplicationOptions _option;
    private readonly IServiceContext _serviceContext;
    private readonly INotifyUserService _userServiceNotification;
    private readonly CompositionContainer _container;
    private bool _isDisposed;
    private CancellationTokenSource? _cancellationTokenSource;
    private string title = string.Empty;

    static Application()
    {
        Logging.LogUtility.Logger = Logging.ConsoleLogger.Default;
    }

#if SERVER
    private readonly bool _isServer = true;
#else
    private readonly bool _isServer = false;
#endif

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
        _userServiceNotification = _container.GetExportedValue<INotifyUserService>();
        _userServiceNotification.LoggedIn += UserServiceNotification_LoggedIn;
        _userServiceNotification.LoggedOut += UserServiceNotification_LoggedOut;
        _userServiceNotification.MessageReceived += UserServiceNotification_MessageReceived;
        Title = "Server";
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

    public void Dispose()
    {
        if (_isDisposed != true)
        {
            _container.Dispose();
            _isDisposed = true;
        }
    }

    internal void Login(string userID, Guid token)
    {
        UserID = userID;
        UserToken = token;
        UpdatePrompt();
        Out.WriteLine("사용자 관련 명령을 수행하려면 'help user' 을(를) 입력하세요.");
    }

    internal void Logout()
    {
        UserID = string.Empty;
        UserToken = Guid.Empty;
        UpdatePrompt();
    }

    internal Guid Token { get; set; }

    internal Guid UserToken { get; private set; }

    internal string UserID { get; private set; } = string.Empty;

    private TextWriter Out => Console.Out;

    private SystemTerminal Terminal => _container.GetExportedValue<SystemTerminal>();

    private void UpdatePrompt()
    {
        var prompt = string.Empty;
        var isOpened = IsOpened;
        var userID = UserID;

        if (isOpened == true)
        {
            prompt = EndPointUtility.GetString(_serviceContext.EndPoint);
            if (userID != string.Empty)
                prompt += $"@{userID}";
        }
        Terminal.Prompt = $"{prompt} {postfix} ";
    }

    private void ServiceContext_Opened(object? sender, EventArgs e)
    {
        IsOpened = true;
        UpdatePrompt();

        if (_isServer)
        {
            Title = $"Server {EndPointUtility.GetString(_serviceContext.EndPoint)}";
            Out.WriteLine("서버가 시작되었습니다.");
        }
        else
        {
            Title = $"Client {EndPointUtility.GetString(_serviceContext.EndPoint)}";
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
        Out.WriteLine($"User logged in: {e.UserID}");
    }

    private void UserServiceNotification_LoggedOut(object? sender, UserEventArgs e)
    {
        Out.WriteLine($"User logged out: {e.UserID}");
    }

    private void UserServiceNotification_MessageReceived(object? sender, UserMessageEventArgs e)
    {
        if (e.Sender == UserID)
        {
            var text = TerminalStringBuilder.GetString($"'{e.Receiver}'에게 귓속말: {e.Message}", TerminalColorType.BrightMagenta);
            Out.WriteLine(text);
        }
        else if (e.Receiver == UserID)
        {
            var text = TerminalStringBuilder.GetString($"'{e.Receiver}'의 귓속말: {e.Message}", TerminalColorType.BrightMagenta);
            Out.WriteLine(text);
        }
    }

    #region IServiceProvider

    object? IServiceProvider.GetService(Type serviceType)
    {
        if (serviceType == typeof(IServiceProvider))
            return this;

        if (typeof(IEnumerable).IsAssignableFrom(serviceType) && serviceType.GenericTypeArguments.Length == 1)
        {
            var itemType = serviceType.GenericTypeArguments.First();
            var contractName = AttributedModelServices.GetContractName(itemType);
            var items = _container.GetExportedValues<object>(contractName);
            var listGenericType = typeof(List<>);
            var list = listGenericType.MakeGenericType(itemType);
            var ci = list.GetConstructor(new Type[] { typeof(int) })!;
            var instance = (IList)ci.Invoke(new object[] { items.Count(), })!;
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

    #endregion

    #region IApplication

    public async Task StartAsync()
    {
        if (_cancellationTokenSource != null)
            throw new InvalidOperationException();

        _cancellationTokenSource = new CancellationTokenSource();
        _serviceContext.EndPoint = new DnsEndPoint(_option.Host, _option.Port);
        try
        {
            Token = await _serviceContext.OpenAsync(_cancellationTokenSource.Token);
        }
        catch (Exception e)
        {
            var text = TerminalStringBuilder.GetString(e.Message, TerminalColorType.BrightRed);
            await _serviceContext.AbortAsync();
        }
        UpdatePrompt();
        await Terminal.StartAsync(_cancellationTokenSource.Token);
    }

    public async Task StopAsync(int exitCode)
    {
        if (_cancellationTokenSource == null)
            throw new InvalidOperationException();

        _cancellationTokenSource.Cancel();
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
    }

    #endregion
}