// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.OpAmp.Client.Messages;

namespace OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;

/// <summary>
/// Adds support for plugins to provide effective configuration
/// </summary>
public interface IProvideEffectiveConfig : IOpAmpPlugin
{
    /// <summary>
    /// Requests plugin to generate effective configuration.
    /// </summary>
    /// <returns>Effective configuration</returns>
    IEnumerable<EffectiveConfigFile> OnEffectiveConfigRequested();
}
