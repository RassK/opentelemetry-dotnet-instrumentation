// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.DuckTyping;

namespace OpenTelemetry.AutoInstrumentation.ByteCode.Instrumentations.ILogger;

internal interface IOTelILogger : IDuckType
{
    /// <summary>
    /// Used to add the ILoggerProvider
    /// </summary>
    [Duck(ParameterTypeNames = new[] { "Microsoft.Extensions.Logging.LogLevel", "Microsoft.Extensions.Logging.EventId", "TState", "System.Exception", "Func`3" })]
    public void Log<TState>(int logLevel, object eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter);
}
