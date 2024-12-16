// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Configurations;

internal class GeneralSettings : Settings
{
    /// <summary>
    /// Gets the list of plugins represented by <see cref="Type.AssemblyQualifiedName"/>.
    /// </summary>
    public IList<string> Plugins { get; } = new List<string>();

    /// <summary>
    /// Gets a value indicating whether the <see cref="AppDomain.UnhandledException"/> event should trigger
    /// the flushing of telemetry data.
    /// Default is <c>false</c>.
    /// </summary>
    public bool FlushOnUnhandledException { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the profiler is enabled.
    /// </summary>
    public bool ProfilerEnabled { get; private set; }

    public bool TracesEnabled { get; private set; }

    public bool MetricsEnabled { get; private set; }

    public bool LogsEnabled { get; private set; }

    protected override void OnLoad(Configuration configuration)
    {
        var providerPlugins = configuration.GetString(ConfigurationKeys.ProviderPlugins);
        if (providerPlugins != null)
        {
            foreach (var pluginAssemblyQualifiedName in providerPlugins.Split(Constants.ConfigurationValues.DotNetQualifiedNameSeparator))
            {
                Plugins.Add(pluginAssemblyQualifiedName);
            }
        }

        FlushOnUnhandledException = configuration.GetBool(ConfigurationKeys.FlushOnUnhandledException) ?? false;

        ProfilerEnabled = configuration.GetString(ConfigurationKeys.ProfilingEnabled) == "1";
        TracesEnabled = configuration.GetString(ConfigurationKeys.Traces.TracesEnabled) == "1";
        MetricsEnabled = configuration.GetString(ConfigurationKeys.Metrics.MetricsEnabled) == "1";
        LogsEnabled = configuration.GetString(ConfigurationKeys.Logs.LogsEnabled) == "1";
    }
}
