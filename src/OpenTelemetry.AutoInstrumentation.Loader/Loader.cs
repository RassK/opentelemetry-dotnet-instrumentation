// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.Loader;

/// <summary>
/// A class that attempts to load the OpenTelemetry.AutoInstrumentation .NET assembly.
/// </summary>
internal partial class Loader
{
    private static readonly string ManagedProfilerDirectory;

    private static readonly IOtelLogger Logger = OtelLogging.GetLogger("Loader");

    /// <summary>
    /// Initializes static members of the <see cref="Loader"/> class.
    /// This method also attempts to load the OpenTelemetry.AutoInstrumentation .NET assembly.
    /// </summary>
    static Loader()
    {
        ManagedProfilerDirectory = ResolveManagedProfilerDirectory();

#if NET
        var mainModulePath = Path.Combine(ManagedProfilerDirectory, "OpenTelemetry.AutoInstrumentation.dll");
        OTelLoadContext = new OTelLoadContext(mainModulePath);
#endif

        try
        {
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve_ManagedProfilerDependencies;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Unable to register a callback to the CurrentDomain.AssemblyResolve event.");
        }

        TryLoadManagedAssembly();
    }

    private static void TryLoadManagedAssembly()
    {
        Logger.Information("Managed Loader TryLoadManagedAssembly()");

        try
        {
            var commonBridge = LoadMainModule();
            LoadByteCodeModule(commonBridge);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error when loading managed assemblies. {0}", ex.Message);
            throw;
        }
    }

    private static object? LoadMainModule()
    {
        var assembly = LoadMainAssembly("OpenTelemetry.AutoInstrumentation");
        if (assembly == null)
        {
            throw new FileNotFoundException("The assembly OpenTelemetry.AutoInstrumentation could not be loaded");
        }

        var type = assembly.GetType("OpenTelemetry.AutoInstrumentation.Instrumentation", throwOnError: false);
        if (type == null)
        {
            throw new TypeLoadException("The type OpenTelemetry.AutoInstrumentation.Instrumentation could not be loaded");
        }

        var method = type.GetRuntimeMethod("Initialize", Type.EmptyTypes);
        if (method == null)
        {
            throw new MissingMethodException("The method OpenTelemetry.AutoInstrumentation.Instrumentation.Initialize could not be loaded");
        }

        var commonBridge = method.Invoke(obj: null, parameters: null);

        return commonBridge;
    }

    private static Assembly LoadByteCodeModule(object? commonBridge)
    {
        var assembly = LoadSharedAssembly("OpenTelemetry.AutoInstrumentation.ByteCode");
        if (assembly == null)
        {
            throw new FileNotFoundException("The assembly OpenTelemetry.AutoInstrumentation.ByteCode could not be loaded");
        }

        var type = assembly.GetType("OpenTelemetry.AutoInstrumentation.ByteCode.Instrumentation", throwOnError: false);
        if (type == null)
        {
            throw new TypeLoadException("The type OpenTelemetry.AutoInstrumentation.ByteCode.Instrumentation could not be loaded");
        }

        var paramsType = Type.GetType("OpenTelemetry.AutoInstrumentation.Bridge.ICommonBridge, OpenTelemetry.AutoInstrumentation.Bridge")!;
        var method = type.GetRuntimeMethod("Initialize", [paramsType]);
        if (method == null)
        {
            throw new MissingMethodException("The method OpenTelemetry.AutoInstrumentation.ByteCode.Instrumentation.Initialize could not be loaded");
        }

        method.Invoke(obj: null, parameters: [commonBridge]);

        return assembly;
    }

    private static string? ReadEnvironmentVariable(string key)
    {
        try
        {
            return Environment.GetEnvironmentVariable(key);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error while loading environment variable {0}", key);
        }

        return null;
    }
}
