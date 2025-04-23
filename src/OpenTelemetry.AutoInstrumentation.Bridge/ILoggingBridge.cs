// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET

namespace OpenTelemetry.AutoInstrumentation.Bridge;

/// <summary>
/// Provides bridge for logging
/// </summary>
public interface ILoggingBridge
{
    /// <summary>
    /// Forward logs
    /// </summary>
    void Log<TState>(int logLevel, int eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter);
}

#endif
