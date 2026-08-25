// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;
using OpenTelemetry.OpAmp.Client.Messages;
using OpenTelemetry.OpAmp.Client.Settings;

namespace OpenTelemetry.AutoInstrumentation.Plugins;

internal partial class PluginManager
{
    public OpAmpPluginCapabilities ConfigurePluginCapabilities()
    {
        return CallPlugins<IOpAmpPlugin, OpAmpPluginCapabilities>(plugin => plugin.ConfigurePluginCapabilities());
    }

    public void ConfigureOpAmpOptions(OpAmpClientSettings options)
    {
        CallPlugins<IOpAmpPlugin>(plugin => plugin.ConfigureOpAmpOptions(options));
    }

    public void AfterOpAmpClientStarted(IOpAmpClient client)
    {
        CallPlugins<IOpAmpPlugin>(plugin => plugin.AfterOpAmpClientStarted(client));
    }

    public void BeforeOpAmpClientStopped()
    {
        CallPlugins<IOpAmpPlugin>(plugin => plugin.BeforeOpAmpClientStopped());
    }

    public IEnumerable<EffectiveConfigFile> RequestEffectiveConfig()
    {
        return CallPlugins<IProvideEffectiveConfig, IEnumerable<EffectiveConfigFile>>(plugin => plugin.OnEffectiveConfigRequested());
    }

    public RemoteConfigStatusReport RequestRemoteConfigStatus()
    {
        return CallPlugins<IProvideRemoteConfigStatus, RemoteConfigStatusReport>(plugin => plugin.OnRemoteConfigStatusRequested());
    }
}
