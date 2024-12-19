// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Reflection;
using OpenTelemetry.AutoInstrumentation.DuckTyping;

namespace OpenTelemetry.AutoInstrumentation.ByteCode.Instrumentations.ILogger;

/// <summary>
/// Duck type for ILoggerProvider
/// </summary>
// [Microsoft.Extensions.Logging.ProviderAlias("OpenTelemetry")]
internal class LoggerProvider
{
    private readonly Func<string, Logger> _createLoggerFunc;
    private readonly ConcurrentDictionary<string, Logger> _loggers = new();
    private IExternalScopeProvider? _scopeProvider;

    internal LoggerProvider(
        IExternalScopeProvider? scopeProvider)
    {
        _createLoggerFunc = CreateLoggerImplementation;
        _scopeProvider = scopeProvider;
    }

    /// <summary>
    /// Creates a new <see cref="ILogger"/> instance.
    /// </summary>
    /// <param name="categoryName">The category name for messages produced by the logger.</param>
    /// <returns>The instance of <see cref="ILogger"/> that was created.</returns>
    [DuckReverseMethod]
    public Logger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, _createLoggerFunc);
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    [DuckReverseMethod]
    public void Dispose()
    {
    }

    /// <summary>
    /// Method for ISupportExternalScope
    /// </summary>
    /// <param name="scopeProvider">The provider of scope data</param>
    [DuckReverseMethod(ParameterTypeNames = new[] { "Microsoft.Extensions.Logging.IExternalScopeProvider, Microsoft.Extensions.Logging.Abstractions" })]
    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    private Logger CreateLoggerImplementation(string name)
    {
        var logger = AppDomain.CurrentDomain.GetAssemblies()
            .First(x => x.GetName().Name == "OpenTelemetry.AutoInstrumentation")
            .GetType("OpenTelemetry.AutoInstrumentation.Instrumentation")!
            .GetProperty("SDKLogBridge", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null);

        var bridge = logger.DuckCast<IOTelLogBridge>()!;

        return new Logger(name, _scopeProvider, bridge);
    }
}
