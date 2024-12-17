// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.ByteCode.Instrumentations.ILogger;

/// <summary>
/// A duck type for Microsoft.Extensions.Logging.IExternalScopeProvider
/// </summary>
internal interface IExternalScopeProvider
{
    /// <summary>
    /// Adds scope object to the list
    /// </summary>
    /// <param name="state">The scope object</param>
    /// <returns>The IDisposable token that removes scope on dispose</returns>
    IDisposable Push(object state);

    /// <summary>
    /// Executes callback for each currently active scope objects in order of creation. All callbacks are guaranteed to be called inline from this method.
    /// Note that original is Action&lt;object, TState&gt; callback
    /// </summary>
    /// <param name="callback">The callback to be executed for every scope object</param>
    /// <param name="state">The state object to be passed into the callback.</param>
    /// <typeparam name="TState">The type of state to accept</typeparam>
    void ForEachScope<TState>(Action<object, TState> callback, TState state);
}
