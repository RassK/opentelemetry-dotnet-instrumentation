// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

/// <summary>
/// Metric Settings
/// </summary>
internal class MetricSettings : Settings
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();

    /// <summary>
    /// Gets a value indicating whether the metrics should be loaded by the profiler. Default is true.
    /// </summary>
    public bool MetricsEnabled { get; private set; }

    /// <summary>
    /// Gets the list of enabled meters.
    /// </summary>
    public IReadOnlyList<MetricInstrumentation> EnabledInstrumentations { get; private set; } = new List<MetricInstrumentation>();

    protected override void OnLoad(Configuration configuration)
    {
        var instrumentationEnabledByDefault =
            configuration.GetBool(ConfigurationKeys.Metrics.MetricsInstrumentationEnabled) ??
            configuration.GetBool(ConfigurationKeys.InstrumentationEnabled) ?? true;

        EnabledInstrumentations = configuration.ParseEnabledEnumList<MetricInstrumentation>(
            enabledByDefault: instrumentationEnabledByDefault,
            enabledConfigurationTemplate: ConfigurationKeys.Metrics.EnabledMetricsInstrumentationTemplate);

        MetricsEnabled = configuration.GetBool(ConfigurationKeys.Metrics.MetricsEnabled) ?? true;
    }
}
