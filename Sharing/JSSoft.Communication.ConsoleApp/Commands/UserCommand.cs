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

using JSSoft.Communication.ConsoleApp;
using JSSoft.Communication.Services;
using JSSoft.Commands;
using System;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using System.Threading;

namespace JSSoft.Communication.Commands;

[Export(typeof(ICommand))]
[method: ImportingConstructor]
class UserCommand(Application application, IUserService userService) : CommandMethodBase
{
    private readonly Application _application = application;
    private readonly IUserService _userService = userService;

    [CommandMethod]
    public Task CreateAsync(string userID, string password, Authority authority, CancellationToken cancellationToken)
    {
        return _userService.CreateAsync(_application.UserToken, userID, password, authority, cancellationToken);
    }

    [CommandMethod]
    public Task DeleteAsync(string userID, CancellationToken cancellationToken)
    {
        return _userService.DeleteAsync(_application.UserToken, userID, cancellationToken);
    }

    [CommandMethod]
    public Task RenameAsync(string userName, CancellationToken cancellationToken)
    {
        return _userService.RenameAsync(_application.UserToken, userName, cancellationToken);
    }

    [CommandMethod]
    public Task AuthorityAsync(string userID, Authority authority, CancellationToken cancellationToken)
    {
        return _userService.SetAuthorityAsync(_application.UserToken, userID, authority, cancellationToken);
    }

    [CommandMethod]
    public async Task InfoAsync(string userID, CancellationToken cancellationToken)
    {
        var (userName, authority) = await _userService.GetInfoAsync(_application.UserToken, userID, cancellationToken);
        Out.WriteLine($"UseName: {userName}");
        Out.WriteLine($"Authority: {authority}");
    }

    [CommandMethod]
    public async Task ListAsync(CancellationToken cancellationToken)
    {
        var items = await _userService.GetUsersAsync(_application.UserToken, cancellationToken);
        foreach (var item in items)
        {
            Out.WriteLine(item);
        }
    }

    [CommandMethod]
    public Task SendMessageAsync(string userID, string message, CancellationToken cancellationToken)
    {
        return _userService.SendMessageAsync(_application.UserToken, userID, message, cancellationToken);
    }

    public override bool IsEnabled => _application.UserToken != Guid.Empty;

    protected override bool IsMethodEnabled(CommandMethodDescriptor descriptor)
    {
        return _application.UserToken != Guid.Empty;
    }
}
