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

using System;
using System.Threading;
using System.Threading.Tasks;

namespace JSSoft.Communication.Services;

[ServiceContract]
public interface IUserService
{
    [ServerMethod]
    Task CreateAsync(Guid token, string userID, string password, Authority authority, CancellationToken cancellationToken);

    [ServerMethod]
    Task DeleteAsync(Guid token, string userID, CancellationToken cancellationToken);

    [ServerMethod]
    Task RenameAsync(Guid token, string userName, CancellationToken cancellationToken);

    [ServerMethod]
    Task SetAuthorityAsync(Guid token, string userID, Authority authority, CancellationToken cancellationToken);

    [ServerMethod]
    Task<Guid> LoginAsync(string userID, string password, CancellationToken cancellationToken);

    [ServerMethod]
    Task LogoutAsync(Guid token, CancellationToken cancellationToken);

    [ServerMethod]
    Task<(string userName, Authority authority)> GetInfoAsync(Guid token, string userID, CancellationToken cancellationToken);

    [ServerMethod]
    Task<string[]> GetUsersAsync(Guid token, CancellationToken cancellationToken);

    [ServerMethod]
    Task<bool> IsOnlineAsync(Guid token, string userID, CancellationToken cancellationToken);

    [ServerMethod]
    Task SendMessageAsync(Guid token, string userID, string message, CancellationToken cancellationToken);
}
