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

using JSSoft.Communication.Logging;
using Xunit.Abstractions;

namespace JSSoft.Communication.Tests;

public sealed class TestLogger(ITestOutputHelper logger) : ILogger
{
    private readonly ITestOutputHelper _logger = logger;

    public void Debug(object message) => _logger.WriteLine($"{message}\n");

    public void Info(object message) => _logger.WriteLine($"{message}\n");

    public void Error(object message) => _logger.WriteLine($"{message}\n");

    public void Warn(object message) => _logger.WriteLine($"{message}\n");

    public void Fatal(object message) => _logger.WriteLine($"{message}\n");
}
