// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.OpAmp.Client.Messages;

namespace OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;

/// <summary>
/// Adds support for plugins to provide remote configuration status
/// </summary>
public interface IProvideRemoteConfigStatus : IOpAmpPlugin
{
    /// <summary>
    /// Requests plugin to provide last applied remote configuration status.
    /// </summary>
    /// <returns>Remote configuration status</returns>
    RemoteConfigStatusReport OnRemoteConfigStatusRequested();
}
