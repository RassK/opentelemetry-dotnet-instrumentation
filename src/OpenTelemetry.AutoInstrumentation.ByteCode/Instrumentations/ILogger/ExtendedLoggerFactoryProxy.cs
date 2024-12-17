// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.DuckTyping;

namespace OpenTelemetry.AutoInstrumentation.ByteCode.Instrumentations.ILogger;

/// <summary>
/// Duck type for https://github.com/dotnet/extensions/blob/e7430144e8009f87ed510e7922c8c780fbb0d9ac/src/Libraries/Microsoft.Extensions.Telemetry/Logging/ExtendedLoggerFactory.cs
/// </summary>
[DuckCopy]
internal struct ExtendedLoggerFactoryProxy
{
    [DuckField(Name = "_scopeProvider")]
    public IExternalScopeProvider? ScopeProvider;
}
