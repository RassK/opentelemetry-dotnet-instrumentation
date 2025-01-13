// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET
using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.ByteCode.ContinuousProfiler;
#endif

using OpenTelemetry.AutoInstrumentation.Bridge;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Loading;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.Plugins;

namespace OpenTelemetry.AutoInstrumentation.ByteCode;

internal static class Instrumentation
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger("ByteCode");

    private static int _initialized;
    private static int _isExiting;
    private static ILifespanManager _lifespanManager = new InstrumentationLifespanManager();

    private static PluginManager? _pluginManager;
    private static ICommonBridge? _commonBridge;

#if NET
    private static ContinuousProfilerProcessor? _profilerProcessor;
#endif

    internal static ILifespanManager LifespanManager => _lifespanManager;

    internal static PluginManager? PluginManager => _pluginManager;

    internal static ICommonBridge? CommonBridge => _commonBridge;

    internal static Lazy<FailFastSettings> FailFastSettings { get; } = new(() => Settings.FromDefaultSources<FailFastSettings>(false));

    internal static Lazy<GeneralSettings> GeneralSettings { get; } = new(() => Settings.FromDefaultSources<GeneralSettings>(FailFastSettings.Value.FailFast));

    internal static Lazy<TracerSettings> TracerSettings { get; } = new(() => Settings.FromDefaultSources<TracerSettings>(FailFastSettings.Value.FailFast));

    internal static Lazy<MetricSettings> MetricSettings { get; } = new(() => Settings.FromDefaultSources<MetricSettings>(FailFastSettings.Value.FailFast));

    internal static Lazy<LogSettings> LogSettings { get; } = new(() => Settings.FromDefaultSources<LogSettings>(FailFastSettings.Value.FailFast));

    public static void Initialize(ICommonBridge pluginState)
    {
        if (Interlocked.Exchange(ref _initialized, value: 1) != 0)
        {
            // Initialize() was already called before
            return;
        }

        _commonBridge = pluginState;

        // Register to shutdown events
        AppDomain.CurrentDomain.ProcessExit += OnExit;
        AppDomain.CurrentDomain.DomainUnload += OnExit;

        _pluginManager = new PluginManager(GeneralSettings.Value);
        _pluginManager.Initializing();

        if (GeneralSettings.Value.FlushOnUnhandledException)
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

#if NET
        var profilerEnabled = GeneralSettings.Value.ProfilerEnabled;

        if (profilerEnabled)
        {
            var (threadSamplingEnabled, threadSamplingInterval, allocationSamplingEnabled, maxMemorySamplesPerMinute, exportInterval, exportTimeout, continuousProfilerExporter) = _pluginManager.GetFirstContinuousConfiguration();
            Logger.Debug($"Continuous profiling configuration: Thread sampling enabled: {threadSamplingEnabled}, thread sampling interval: {threadSamplingInterval}, allocation sampling enabled: {allocationSamplingEnabled}, max memory samples per minute: {maxMemorySamplesPerMinute}, export interval: {exportInterval}, export timeout: {exportTimeout}, continuous profiler exporter: {continuousProfilerExporter.GetType()}");

            if (threadSamplingEnabled || allocationSamplingEnabled)
            {
                InitializeContinuousProfiling(continuousProfilerExporter, threadSamplingEnabled, allocationSamplingEnabled, threadSamplingInterval, maxMemorySamplesPerMinute, exportInterval, exportTimeout);
            }
        }
        else
        {
            Logger.Information("CLR Profiler is not enabled. Continuous Profiler will be not started even if configured correctly.");
        }
#endif

        if (GeneralSettings.Value.ProfilerEnabled)
        {
            RegisterBytecodeInstrumentations(InstrumentationDefinitions.GetAllDefinitions());

            try
            {
                foreach (var payload in _pluginManager.GetAllDefinitionsPayloads())
                {
                    RegisterBytecodeInstrumentations(payload);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Exception occurred while registering instrumentations from plugins.");
            }

            try
            {
                Logger.Debug("Sending CallTarget derived integration definitions to native library.");
                var payload = InstrumentationDefinitions.GetDerivedDefinitions();
                NativeMethods.AddDerivedInstrumentations(payload.DefinitionsId, payload.Definitions);
                foreach (var def in payload.Definitions)
                {
                    def.Dispose();
                }

                Logger.Information("The profiler has been initialized with {0} derived definitions.", payload.Definitions.Length);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, ex.Message);
            }
        }
        else
        {
            Logger.Debug("Skipping CLR Profiler initialization. {0} environment variable was not set to '1'.", ConfigurationKeys.ProfilingEnabled);
        }
    }

    private static void RegisterBytecodeInstrumentations(InstrumentationDefinitions.Payload payload)
    {
        try
        {
            Logger.Debug("Sending CallTarget integration definitions to native library for {0}.", payload.DefinitionsId);
            NativeMethods.AddInstrumentations(payload.DefinitionsId, payload.Definitions);
            foreach (var def in payload.Definitions)
            {
                def.Dispose();
            }

            Logger.Information("The profiler has been initialized with {0} definitions for {1}.", payload.Definitions.Length, payload.DefinitionsId);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, ex.Message);
        }
    }

#if NET
    private static void InitializeContinuousProfiling(
        object continuousProfilerExporter,
        bool threadSamplingEnabled,
        bool allocationSamplingEnabled,
        uint threadSamplingInterval,
        uint maxMemorySamplesPerMinute,
        TimeSpan exportInterval,
        TimeSpan exportTimeout)
    {
        var continuousProfilerExporterType = continuousProfilerExporter.GetType();
        var exportThreadSamplesMethod = continuousProfilerExporterType.GetMethod("ExportThreadSamples");

        if (exportThreadSamplesMethod == null)
        {
            Logger.Warning("Exporter does not have ExportThreadSamples method. Continuous Profiler initialization failed.");
            return;
        }

        var exportAllocationSamplesMethod = continuousProfilerExporterType.GetMethod("ExportAllocationSamples");
        if (exportAllocationSamplesMethod == null)
        {
            Logger.Warning("Exporter does not have ExportAllocationSamples method. Continuous Profiler initialization failed.");
            return;
        }

        NativeMethods.ConfigureNativeContinuousProfiler(threadSamplingEnabled, threadSamplingInterval, allocationSamplingEnabled, maxMemorySamplesPerMinute);
        var threadSamplesMethod = exportThreadSamplesMethod.CreateDelegate<Action<byte[], int, CancellationToken>>(continuousProfilerExporter);
        var allocationSamplesMethod = exportAllocationSamplesMethod.CreateDelegate<Action<byte[], int, CancellationToken>>(continuousProfilerExporter);

        var bufferProcessor = new BufferProcessor(threadSamplingEnabled, allocationSamplingEnabled, threadSamplesMethod, allocationSamplesMethod, exportTimeout);
        _profilerProcessor = new ContinuousProfilerProcessor(bufferProcessor, exportInterval, exportTimeout);
        Activity.CurrentChanged += _profilerProcessor.Activity_CurrentChanged;
    }
#endif

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
            if (_profilerProcessor != null)
            {
                Activity.CurrentChanged -= _profilerProcessor.Activity_CurrentChanged;
                _profilerProcessor.Dispose();
            }
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
