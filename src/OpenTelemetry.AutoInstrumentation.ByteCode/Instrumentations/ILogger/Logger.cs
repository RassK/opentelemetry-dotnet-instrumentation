// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using OpenTelemetry.AutoInstrumentation.DuckTyping;

namespace OpenTelemetry.AutoInstrumentation.ByteCode.Instrumentations.ILogger;

/// <summary>
/// An implementation of ILogger for use with direct log submission
/// </summary>
internal class Logger
{
    private readonly string _name;
    private readonly IExternalScopeProvider? _scopeProvider;
    private readonly IOTelILogger _sink;
    private readonly int _minimumLogLevel = 1;

    internal Logger(
        string name,
        IExternalScopeProvider? scopeProvider,
        IOTelILogger sink)
    {
        _name = name;
        _scopeProvider = scopeProvider;
        _sink = sink;
    }

    /// <summary>
    /// Writes a log entry.
    /// </summary>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">Id of the event.</param>
    /// <param name="state">The entry to be written. Can be also an object.</param>
    /// <param name="exception">The exception related to this entry.</param>
    /// <param name="formatter">Function to create a <see cref="string"/> message of the <paramref name="state"/> and <paramref name="exception"/>.</param>
    /// <typeparam name="TState">The type of the object to be written.</typeparam>
    [DuckReverseMethod(ParameterTypeNames = new[] { "Microsoft.Extensions.Logging.LogLevel", "Microsoft.Extensions.Logging.EventId", "TState", "System.Exception", "Func`3" })]
    public void Log<TState>(int logLevel, object eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        _sink.Log(logLevel, eventId, state, exception, formatter);
    }

    /// <summary>
    /// Checks if the given <paramref name="logLevel"/> is enabled.
    /// </summary>
    /// <param name="logLevel">Level to be checked.</param>
    /// <returns><c>true</c> if enabled.</returns>
    [DuckReverseMethod(ParameterTypeNames = new[] { "Microsoft.Extensions.Logging.LogLevel, Microsoft.Extensions.Logging.Abstractions" })]
    public bool IsEnabled(int logLevel) => logLevel >= _minimumLogLevel;

    /// <summary>
    /// Begins a logical operation scope.
    /// </summary>
    /// <param name="state">The identifier for the scope.</param>
    /// <typeparam name="TState">The type of the state to begin scope for.</typeparam>
    /// <returns>An <see cref="IDisposable"/> that ends the logical operation scope on dispose.</returns>
    [DuckReverseMethod(ParameterTypeNames = new[] { "TState" })]
    public IDisposable BeginScope<TState>(TState state) => _scopeProvider?.Push(state!) ?? NullDisposable.Instance;

    private class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new();

        public void Dispose()
        {
        }
    }
}
