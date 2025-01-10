// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET

using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace OpenTelemetry.AutoInstrumentation.Loader;

internal class OTelLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver resolver;

    public OTelLoadContext(string pluginModule)
    {
        resolver = new AssemblyDependencyResolver(pluginModule);
    }

    internal static string[]? StoreFiles { get; } = GetStoreFiles();

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (assemblyName.Name == "System.Diagnostics.DiagnosticSource" ||
            assemblyName.Name == "OpenTelemetry.AutoInstrumentation.Bridge")
        {
            return Default.LoadFromAssemblyName(assemblyName);
        }

        string? assemblyPath = resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        var entry = StoreFiles?.FirstOrDefault(e => e.EndsWith($"{assemblyName.Name}.dll"));
        if (entry != null)
        {
            return LoadFromAssemblyPath(entry);
        }

        return null;
    }

    private static string[]? GetStoreFiles()
    {
        try
        {
            var storeDirectory = Environment.GetEnvironmentVariable("DOTNET_SHARED_STORE");
            if (storeDirectory == null || !Directory.Exists(storeDirectory))
            {
                return null;
            }

            var architecture = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X86 => "x86",
                Architecture.Arm64 => "arm64",
                _ => "x64" // Default to x64 for architectures not explicitly handled
            };

            var targetFramework = $"net{Environment.Version.Major}.{Environment.Version.Minor}";
            var finalPath = Path.Combine(storeDirectory, architecture, targetFramework);

            var storeFiles = Directory.GetFiles(finalPath, "Microsoft.Extensions*.dll", SearchOption.AllDirectories);
            return storeFiles;
        }
        catch
        {
            return null;
        }
    }
}

#endif
