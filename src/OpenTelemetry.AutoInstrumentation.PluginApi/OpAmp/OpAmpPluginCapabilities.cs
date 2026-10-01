// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;

/// <summary>
/// Plugin capabilities
/// </summary>
[Flags]
public enum OpAmpPluginCapabilities
{
    /// <summary>
    /// Plugin has no capabilities.
    /// </summary>
    None = 0,

    /// <summary>
    /// Plugin can override effective config reports.
    /// </summary>
    ReportsEffectiveConfig = 1,

    /// <summary>
    /// Plugin can override remote config status reports.
    /// </summary>
    ReportsRemoteConfigStatus = 2,
}
