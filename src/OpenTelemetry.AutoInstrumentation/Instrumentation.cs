// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET
using OpenTelemetry.AutoInstrumentation.Logger;
#endif
using System.Reflection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Diagnostics;
using OpenTelemetry.AutoInstrumentation.Loading;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.Plugins;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace OpenTelemetry.AutoInstrumentation;

/// <summary>
/// Instrumentation
/// </summary>
internal static class Instrumentation
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();
    private static readonly LazyInstrumentationLoader LazyInstrumentationLoader = new();

    private static readonly Lazy<LoggerProvider?> LoggerProviderFactory = new(InitializeLoggerProvider, true);

    private static int _initialized;
    private static int _isExiting;
    private static SdkSelfDiagnosticsEventListener? _sdkEventListener;

    private static TracerProvider? _tracerProvider;
    private static MeterProvider? _meterProvider;

    private static PluginManager? _pluginManager;

#if NET
    private static ILogger? _sdkLogBridge;
    private static ILoggerFactory? _sdkLogBridgeFactory;
#endif

    internal static LoggerProvider? LoggerProvider
    {
        get => LoggerProviderFactory.Value;
    }

    internal static PluginManager? PluginManager => _pluginManager;

    internal static ILifespanManager LifespanManager => LazyInstrumentationLoader.LifespanManager;

    internal static Lazy<FailFastSettings> FailFastSettings { get; } = new(() => Settings.FromDefaultSources<FailFastSettings>(false));

    internal static Lazy<GeneralSettings> GeneralSettings { get; } = new(() => Settings.FromDefaultSources<GeneralSettings>(FailFastSettings.Value.FailFast));

    internal static Lazy<TracerSettings> TracerSettings { get; } = new(() => Settings.FromDefaultSources<TracerSettings>(FailFastSettings.Value.FailFast));

    internal static Lazy<MetricSettings> MetricSettings { get; } = new(() => Settings.FromDefaultSources<MetricSettings>(FailFastSettings.Value.FailFast));

    internal static Lazy<LogSettings> LogSettings { get; } = new(() => Settings.FromDefaultSources<LogSettings>(FailFastSettings.Value.FailFast));

    internal static Lazy<SdkSettings> SdkSettings { get; } = new(() => Settings.FromDefaultSources<SdkSettings>(FailFastSettings.Value.FailFast));

#if NET
    internal static ILogger? SDKLogBridge
    {
        get => _sdkLogBridge;
    }
#endif

    /// <summary>
    /// Initialize the OpenTelemetry SDK with a pre-defined set of exporters, shims, and
    /// instrumentations.
    /// </summary>
    public static void Initialize()
    {
        if (Interlocked.Exchange(ref _initialized, value: 1) != 0)
        {
            // Initialize() was already called before
            return;
        }

#if NETFRAMEWORK
        try
        {
            // On .NET Framework only, initialize env vars from app.config/web.config
            // this does not override settings which where already set via env vars.
            // We are doing so as the OTel .NET SDK only supports the env vars and we want to
            // be able to set them via app.config/web.config.
            EnvironmentInitializer.Initialize(System.Configuration.ConfigurationManager.AppSettings);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to initialize from AppSettings.");
            throw;
        }
#endif

        try
        {
            // Initialize SdkSelfDiagnosticsEventListener to create an EventListener for the OpenTelemetry SDK
            _sdkEventListener = new(Logger);

            _pluginManager = new PluginManager(GeneralSettings.Value);
            _pluginManager.Initializing();

            if (TracerSettings.Value.TracesEnabled || MetricSettings.Value.MetricsEnabled)
            {
                // Register to shutdown events
                AppDomain.CurrentDomain.ProcessExit += OnExit;
                AppDomain.CurrentDomain.DomainUnload += OnExit;

                if (GeneralSettings.Value.FlushOnUnhandledException)
                {
                    AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
                }

                EnvironmentConfigurationSdkHelper.UseEnvironmentVariables(SdkSettings.Value);
            }

            if (TracerSettings.Value.TracesEnabled)
            {
                if (GeneralSettings.Value.SetupSdk)
                {
                    var builder = Sdk
                        .CreateTracerProviderBuilder()
                        .InvokePluginsBefore(_pluginManager)
                        .SetResourceBuilder(ResourceConfigurator.CreateResourceBuilder(GeneralSettings.Value.EnabledResourceDetectors))
                        .UseEnvironmentVariables(LazyInstrumentationLoader, TracerSettings.Value, _pluginManager)
                        .InvokePluginsAfter(_pluginManager);

                    _tracerProvider = builder.Build();
                    _tracerProvider.TryCallInitialized(_pluginManager);
                    Logger.Information("OpenTelemetry tracer initialized.");
                }
                else
                {
                    AddLazilyLoadedTraceInstrumentations(LazyInstrumentationLoader, _pluginManager, TracerSettings.Value);
                    Logger.Information("Initialized lazily-loaded trace instrumentations without initializing sdk.");
                }
            }

            if (MetricSettings.Value.MetricsEnabled)
            {
                if (GeneralSettings.Value.SetupSdk)
                {
                    var builder = Sdk
                        .CreateMeterProviderBuilder()
                        .InvokePluginsBefore(_pluginManager)
                        .SetResourceBuilder(ResourceConfigurator.CreateResourceBuilder(GeneralSettings.Value.EnabledResourceDetectors))
                        .UseEnvironmentVariables(LazyInstrumentationLoader, MetricSettings.Value, _pluginManager)
                        .InvokePluginsAfter(_pluginManager);

                    _meterProvider = builder.Build();
                    _meterProvider.TryCallInitialized(_pluginManager);
                    Logger.Information("OpenTelemetry meter initialized.");
                }
                else
                {
                    AddLazilyLoadedMetricInstrumentations(LazyInstrumentationLoader, _pluginManager, MetricSettings.Value.EnabledInstrumentations);

                    Logger.Information("Initialized lazily-loaded metric instrumentations without initializing sdk.");
                }
            }

#if NET
            if (LogSettings.Value.LogsEnabled)
            {
                _sdkLogBridgeFactory = LoggerFactory.Create(builder => builder
                    .AddOpenTelemetryLogsFromStartup());

                _sdkLogBridge = _sdkLogBridgeFactory
                    .CreateLogger("OpenTelemetry");
            }
#endif
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "OpenTelemetry SDK load exception.");
            throw;
        }

        if (TracerSettings.Value.OpenTracingEnabled)
        {
            OpenTracingHelper.EnableOpenTracing(_tracerProvider);
        }
    }

    private static LoggerProvider? InitializeLoggerProvider()
    {
        // ILogger bridge is initialized using ILogger-specific extension methods in LoggerInitializer class.
        // That extension methods sets up its own LogProvider.
        if (LogSettings.Value.EnableLog4NetBridge && LogSettings.Value.LogsEnabled && LogSettings.Value.EnabledInstrumentations.Contains(LogInstrumentation.Log4Net))
        {
            // TODO: Replace reflection usage when Logs Api is made public in non-rc builds.
            // Sdk.CreateLoggerProviderBuilder()
            var createLoggerProviderBuilderMethod = typeof(Sdk).GetMethod("CreateLoggerProviderBuilder", BindingFlags.Static | BindingFlags.NonPublic)!;
            var loggerProviderBuilder = createLoggerProviderBuilderMethod.Invoke(null, null) as LoggerProviderBuilder;

            // TODO: plugins support
            var loggerProvider = loggerProviderBuilder!
                .SetResourceBuilder(ResourceConfigurator.CreateResourceBuilder(GeneralSettings.Value.EnabledResourceDetectors))
                .UseEnvironmentVariables(LazyInstrumentationLoader, LogSettings.Value, _pluginManager!)
                .Build();
            Logger.Information("OpenTelemetry logger provider initialized.");
            return loggerProvider;
        }

        return null;
    }

    private static void AddLazilyLoadedMetricInstrumentations(LazyInstrumentationLoader lazyInstrumentationLoader, PluginManager pluginManager, IReadOnlyList<MetricInstrumentation> enabledInstrumentations)
    {
        foreach (var instrumentation in enabledInstrumentations)
        {
            switch (instrumentation)
            {
#if NETFRAMEWORK
                case MetricInstrumentation.AspNet:
                    DelayedInitialization.Metrics.AddAspNet(lazyInstrumentationLoader, pluginManager);
                    break;
#endif
#if NET
                case MetricInstrumentation.AspNetCore:
                    break;
#endif
                case MetricInstrumentation.HttpClient:
                    DelayedInitialization.Metrics.AddHttpClient(lazyInstrumentationLoader);
                    break;
                case MetricInstrumentation.NetRuntime:
                    break;
                case MetricInstrumentation.Process:
                    break;
                case MetricInstrumentation.NServiceBus:
                    break;
                case MetricInstrumentation.SqlClient:
                    DelayedInitialization.Metrics.AddSqlClient(lazyInstrumentationLoader, pluginManager);
                    break;
                default:
                    Logger.Warning($"Configured metric instrumentation type is not supported: {instrumentation}");
                    if (FailFastSettings.Value.FailFast)
                    {
                        throw new NotSupportedException($"Configured metric instrumentation type is not supported: {instrumentation}");
                    }

                    break;
            }
        }
    }

    private static void AddLazilyLoadedTraceInstrumentations(LazyInstrumentationLoader lazyInstrumentationLoader, PluginManager pluginManager, TracerSettings tracerSettings)
    {
        foreach (var instrumentation in tracerSettings.EnabledInstrumentations)
        {
            switch (instrumentation)
            {
#if NETFRAMEWORK
                case TracerInstrumentation.AspNet:
                    DelayedInitialization.Traces.AddAspNet(lazyInstrumentationLoader, pluginManager, tracerSettings);
                    break;
                case TracerInstrumentation.WcfService:
                    break;
#endif
                case TracerInstrumentation.HttpClient:
                    DelayedInitialization.Traces.AddHttpClient(lazyInstrumentationLoader, pluginManager, tracerSettings);
                    break;
                case TracerInstrumentation.GrpcNetClient:
                    DelayedInitialization.Traces.AddGrpcClient(lazyInstrumentationLoader, pluginManager, tracerSettings);
                    break;
                case TracerInstrumentation.SqlClient:
                    DelayedInitialization.Traces.AddSqlClient(lazyInstrumentationLoader, pluginManager, tracerSettings);
                    break;
                case TracerInstrumentation.Quartz:
                    DelayedInitialization.Traces.AddQuartz(lazyInstrumentationLoader, pluginManager);
                    break;
                case TracerInstrumentation.WcfClient:
                    break;
#if NET
                case TracerInstrumentation.AspNetCore:
                    DelayedInitialization.Traces.AddAspNetCore(lazyInstrumentationLoader, pluginManager, tracerSettings);
                    break;
                case TracerInstrumentation.MySqlData:
                    break;
                case TracerInstrumentation.EntityFrameworkCore:
                    DelayedInitialization.Traces.AddEntityFrameworkCore(LazyInstrumentationLoader, pluginManager, tracerSettings);
                    break;
                case TracerInstrumentation.StackExchangeRedis:
                    break;
                case TracerInstrumentation.MassTransit:
                    break;
                case TracerInstrumentation.GraphQL:
                    DelayedInitialization.Traces.AddGraphQL(LazyInstrumentationLoader, pluginManager, tracerSettings);
                    break;
#endif
                case TracerInstrumentation.Azure:
                    break;
                case TracerInstrumentation.MongoDB:
                    break;
                case TracerInstrumentation.Npgsql:
                    break;
                case TracerInstrumentation.NServiceBus:
                    break;
                case TracerInstrumentation.Elasticsearch:
                    break;
                case TracerInstrumentation.ElasticTransport:
                    break;
                case TracerInstrumentation.MySqlConnector:
                    break;
                case TracerInstrumentation.Kafka:
                    break;
                case TracerInstrumentation.OracleMda:
                    break;
                case TracerInstrumentation.RabbitMq:
                    break;
                default:
                    Logger.Warning($"Configured trace instrumentation type is not supported: {instrumentation}");
                    if (FailFastSettings.Value.FailFast)
                    {
                        throw new NotSupportedException($"Configured trace instrumentation type is not supported: {instrumentation}");
                    }

                    break;
            }
        }
    }

    private static void OnExit(object? sender, EventArgs e)
    {
        if (Interlocked.Exchange(ref _isExiting, value: 1) != 0)
        {
            // OnExit() was already called before
            return;
        }

        try
        {
#if NET
            LazyInstrumentationLoader?.Dispose();
#endif
            _tracerProvider?.Dispose();
            _meterProvider?.Dispose();
            if (LoggerProviderFactory.IsValueCreated)
            {
                LoggerProvider?.Dispose();
            }

            _sdkEventListener?.Dispose();

#if NET
            _sdkLogBridgeFactory?.Dispose();
#endif

            Logger.Information("OpenTelemetry Automatic Instrumentation exit.");
        }
        catch (Exception ex)
        {
            try
            {
                Logger.Error(ex, "An error occurred while attempting to exit.");
            }
            catch
            {
                // If we encounter an error while logging there is nothing else we can do
                // with the exception.
            }
        }
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
    {
        try
        {
            if (args.IsTerminating)
            {
                Logger.Error("UnhandledException event raised with a terminating exception.");
                OnExit(sender, args);
            }
        }
        catch (Exception ex)
        {
            try
            {
                Logger.Error(ex, "An exception occurred while processing an unhandled exception.");
            }
            catch
            {
                // If we encounter an error while logging there is nothing else we can do
                // with the exception.
            }
        }
    }
}
