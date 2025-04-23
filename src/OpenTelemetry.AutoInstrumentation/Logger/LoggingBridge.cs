// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET

using Microsoft.Extensions.Logging;
using OpenTelemetry.AutoInstrumentation.Bridge;

namespace OpenTelemetry.AutoInstrumentation.Logger;

internal class LoggingBridge : ILoggingBridge
{
    private readonly ILogger _logger;

    public LoggingBridge(ILogger logger)
    {
        _logger = logger;
    }

    public void Log<TState>(int logLevel, int eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _logger.Log((LogLevel)logLevel, eventId, state, exception, formatter);
    }
}

#endif
