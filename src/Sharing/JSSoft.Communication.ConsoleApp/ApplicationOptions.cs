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

using JSSoft.Commands;
using System.ComponentModel.Composition;

namespace JSSoft.Communication.ConsoleApp;

[Export]
public class ApplicationOptions
{
    [CommandPropertyRequired(DefaultValue = ServiceContextBase.DefaultHost)]
    public string Host { get; set; } = string.Empty;

    [CommandProperty(InitValue = ServiceContextBase.DefaultPort)]
    public int Port { get; set; }

    [CommandPropertySwitch]
    public bool Verbose { get; set; }

    public static ApplicationOptions Parse(string[] args)
    {
        var options = new ApplicationOptions();
        var parserSettings = new CommandSettings()
        {
            AllowEmpty = true,
        };
        var parser = new CommandParser(options, parserSettings);
        parser.Parse(args);
        return options;
    }
}