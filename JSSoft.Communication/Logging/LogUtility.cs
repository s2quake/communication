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

namespace JSSoft.Communication.Logging;

public static class LogUtility
{
    public static ILogger Logger { get; set; } = TraceLogger.Default;

    public static LogLevel LogLevel { get; set; } = LogLevel.Fatal;

    public static void Debug(object message)
    {
        if (LogLevel >= LogLevel.Debug)
            LoggerInternal.Debug(message);
    }

    public static void Info(object message)
    {
        if (LogLevel >= LogLevel.Info)
            LoggerInternal.Info(message);
    }

    public static void Error(object message)
    {
        if (LogLevel >= LogLevel.Error)
            LoggerInternal.Error(message);
    }

    public static void Warn(object message)
    {
        if (LogLevel >= LogLevel.Warn)
            LoggerInternal.Warn(message);
    }

    public static void Fatal(object message)
    {
        if (LogLevel >= LogLevel.Fatal)
            LoggerInternal.Fatal(message);
    }

    private static ILogger LoggerInternal => Logger ?? EmptyLogger.Default;
}
