// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET

using System.Reflection;
using System.Reflection.Emit;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.ByteCode.Instrumentations.ILogger;

internal static class LoggerFactoryIntegrationCommon<TLoggerFactory>
{
    internal static readonly Type? ProviderInterfaces;

    private static readonly IOtelLogger Log = OtelLogging.GetLogger(nameof(LoggerFactoryIntegrationCommon<TLoggerFactory>));

    static LoggerFactoryIntegrationCommon()
    {
        try
        {
            // The ILoggerProvider type is in a different assembly to the LoggerFactory, so go via the ILogger type
            // returned by CreateLogger
            var loggerFactoryType = typeof(TLoggerFactory);
            var abstractionsAssembly = loggerFactoryType.GetMethod("CreateLogger")!.ReturnType.Assembly;
            var iLoggerProviderType = abstractionsAssembly.GetType("Microsoft.Extensions.Logging.ILoggerProvider");
            var iSupportExternalScopeType = abstractionsAssembly.GetType("Microsoft.Extensions.Logging.ISupportExternalScope")!;

            // We need to implement both ILoggerProvider and ISupportExternalScope
            // because LoggerFactory uses pattern matching to check if we implement the latter
            // Duck Typing can currently only implement a single interface, so emit
            // a new interface that implements both ILoggerProvider and ISupportExternalScope
            // and duck cast to that
            var thisAssembly = typeof(LoggerProvider).Assembly;
            var assemblyName = new AssemblyName("OpenTelemetry.AutoInstrumentation.DirectLogSubmissionILoggerFactoryAssembly") { Version = thisAssembly.GetName().Version };

            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");

            var typeBuilder = moduleBuilder.DefineType(
                "LoggerProviderProxy",
                TypeAttributes.Interface | TypeAttributes.Public | TypeAttributes.Abstract,
                parent: null,
                interfaces: new[] { iLoggerProviderType!, iSupportExternalScopeType });

            ProviderInterfaces = typeBuilder.CreateTypeInfo()!.AsType();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error loading logger factory types for {LoggerFactoryType}", typeof(TLoggerFactory));
            ProviderInterfaces = null;
        }
    }

    internal static bool TryAddDirectSubmissionLoggerProvider(TLoggerFactory loggerFactory)
        => TryAddDirectSubmissionLoggerProvider(loggerFactory, scopeProvider: null);

    internal static bool TryAddDirectSubmissionLoggerProvider(TLoggerFactory loggerFactory, IExternalScopeProvider? scopeProvider)
    {
        if (ProviderInterfaces is null)
        {
            // there was a problem loading the assembly for some reason
            return false;
        }

        var provider = new LoggerProvider(
            scopeProvider);

        return TryAddDirectSubmissionLoggerProvider(loggerFactory, provider);
    }

    // Internal for testing
    internal static bool TryAddDirectSubmissionLoggerProvider(TLoggerFactory loggerFactory, LoggerProvider provider)
    {
        if (ProviderInterfaces is null)
        {
            // there was a problem loading the assembly for some reason
            return false;
        }

        var proxy = provider.DuckImplement(ProviderInterfaces);
        if (loggerFactory is not null)
        {
            var loggerFactoryProxy = loggerFactory.DuckCast<ILoggerFactory>();
            loggerFactoryProxy.AddProvider(proxy);
            Log.Information("OpenTelemetry ILogger integration enabled");
            return true;
        }

        return false;
    }
}

#endif

