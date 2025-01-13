// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Bridge;

namespace OpenTelemetry.AutoInstrumentation;

#pragma warning disable SA1600 // Elements should be documented

public static class Instrumentation
{
    public static ICommonBridge Initialize()
    {
        Console.WriteLine($"{nameof(Initialize)} called");

        return new CommonBridge();
    }

    public class CommonBridge : ICommonBridge
    {
#if NET
        public ILoggingBridge? LoggingBridge => new LoggingBridge();
#endif
    }

#if NET
    public class LoggingBridge : ILoggingBridge
    {
        public void Log<TState>(int logLevel, int eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Console.WriteLine("LoggingBridge.Log Mock");
        }
    }
#endif
}
