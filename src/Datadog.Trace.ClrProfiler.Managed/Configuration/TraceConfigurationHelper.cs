using System;
using Datadog.Trace.Configuration;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Datadog.Trace.ClrProfiler.Configuration
{
    internal static class TraceConfigurationHelper
    {
        public static TracerProviderBuilder UseEnvironmentVariables(this TracerProviderBuilder builder, TracerSettings settings)
        {
            return builder.SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService(settings.ServiceName, serviceVersion: settings.ServiceVersion))
                .SetExporter(settings);
        }

        public static TracerProviderBuilder AddAspNetInstrumentation(this TracerProviderBuilder builder, TracerSettings settings)
        {
#if NETFRAMEWORK
            return builder.AddAspNetInstrumentation();
#else
            return builder.AddAspNetCoreInstrumentation();
#endif
        }

        private static TracerProviderBuilder SetExporter(this TracerProviderBuilder builder, TracerSettings settings)
        {
            switch (settings.Exporter)
            {
                case ExporterType.Zipkin:
                    builder.AddZipkinExporter(options =>
                    {
                        options.Endpoint = settings.AgentUri;
                    });

                    break;
                case ExporterType.Jaeger:
#if NET452
                    throw new NotSupportedException();
#else
                    var agentHost = settings.ConfigurationSource?.GetString(ConfigurationKeys.JaegerExporterAgentHost) ?? "localhost";
                    var agentPort = settings.ConfigurationSource?.GetInt32(ConfigurationKeys.JaegerExporterAgentPort) ?? 6831;

                    builder.AddJaegerExporter(options =>
                    {
                        options.AgentHost = agentHost;
                        options.AgentPort = agentPort;
                    });

                    break;
#endif
            }

            return builder;
        }
    }
}
