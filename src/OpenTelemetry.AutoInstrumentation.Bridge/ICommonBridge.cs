// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Bridge;

/// <summary>
/// Forwards plugin state
/// </summary>
public interface ICommonBridge
{
#if NET
    /// <summary>
    /// Gets get the bridge
    /// </summary>
    ILoggingBridge? LoggingBridge { get; }
#endif
}
