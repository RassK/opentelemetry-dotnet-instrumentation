// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.DuckTyping;

namespace OpenTelemetry.AutoInstrumentation.ByteCode.Instrumentations.ILogger;

/// <summary>
/// Duck type for ILogLevel
/// </summary>
internal interface ILoggerFactory : IDuckType
{
    /// <summary>
    /// Used to add the ILoggerProvider
    /// </summary>
    /// <param name="provider">The ILoggerProvider to add</param>
    [Duck]
    public void AddProvider(object provider);
}
