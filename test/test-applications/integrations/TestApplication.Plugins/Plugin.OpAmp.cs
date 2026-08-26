// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.PluginApi;
using OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;
using OpenTelemetry.OpAmp.Client.Messages;
using OpenTelemetry.OpAmp.Client.Settings;

namespace TestApplication.Plugins;

#pragma warning disable CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
/// <summary>
/// OpAMP extensions of the plugin.
/// </summary>
public partial class Plugin : IPlugin, IOpAmpPlugin, IProvideEffectiveConfig, IProvideRemoteConfigStatus
#pragma warning restore CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
{
    public void ConfigureOpAmpOptions(OpAmpClientSettings settings)
    {
        Console.WriteLine($"{nameof(Plugin)}.{nameof(ConfigureOpAmpOptions)}() invoked.");
    }

    public void BeforeOpAmpClientStopped()
    {
        Console.WriteLine($"{nameof(Plugin)}.{nameof(BeforeOpAmpClientStopped)}() invoked.");
    }

    public OpAmpPluginCapabilities ConfigurePluginCapabilities()
    {
        Console.WriteLine($"{nameof(Plugin)}.{nameof(ConfigurePluginCapabilities)}() invoked.");

        return OpAmpPluginCapabilities.ReportsEffectiveConfig |
            OpAmpPluginCapabilities.ReportsRemoteConfigStatus;
    }

    public void AfterOpAmpClientStarted(IOpAmpClient client)
    {
        Console.WriteLine($"{nameof(Plugin)}.{nameof(AfterOpAmpClientStarted)}() invoked.");
    }

    public IEnumerable<EffectiveConfigFile> OnEffectiveConfigRequested()
    {
        Console.WriteLine($"{nameof(Plugin)}.{nameof(OnEffectiveConfigRequested)}() invoked.");

        return [];
    }

    public RemoteConfigStatusReport OnRemoteConfigStatusRequested()
    {
        Console.WriteLine($"{nameof(Plugin)}.{nameof(OnRemoteConfigStatusRequested)}() invoked.");

        return new RemoteConfigStatusReport("temporary"u8, RemoteConfigStatusCode.Unset);
    }
}
