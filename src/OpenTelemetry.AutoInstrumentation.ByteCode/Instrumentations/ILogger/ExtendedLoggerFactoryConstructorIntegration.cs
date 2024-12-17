// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET

using System.ComponentModel;
using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.DuckTyping;

namespace OpenTelemetry.AutoInstrumentation.ByteCode.Instrumentations.ILogger;

/// <summary>
/// LoggerFactory() calltarget instrumentation
/// </summary>
[InstrumentMethod(
    assemblyName: "Microsoft.Extensions.Telemetry",
    typeName: "Microsoft.Extensions.Logging.ExtendedLoggerFactory",
    methodName: ".ctor",
    returnTypeName: ClrNames.Void,
    parameterTypeNames: new[] { "System.Collections.Generic.IEnumerable`1[Microsoft.Extensions.Logging.ILoggerProvider]", "System.Collections.Generic.IEnumerable`1[Microsoft.Extensions.Diagnostics.Enrichment.ILogEnricher]", "System.Collections.Generic.IEnumerable`1[Microsoft.Extensions.Diagnostics.Enrichment.IStaticLogEnricher]", "Microsoft.Extensions.Options.IOptionsMonitor`1[Microsoft.Extensions.Logging.LoggerFilterOptions]", "Microsoft.Extensions.Options.IOptions`1[Microsoft.Extensions.Logging.LoggerFactoryOptions]", "Microsoft.Extensions.Logging.IExternalScopeProvider", "Microsoft.Extensions.Options.IOptionsMonitor`1[Microsoft.Extensions.Logging.LoggerEnrichmentOptions]", "Microsoft.Extensions.Options.IOptionsMonitor`1[Microsoft.Extensions.Logging.LoggerRedactionOptions]", "Microsoft.Extensions.Compliance.Redaction.IRedactorProvider" },
    minimumVersion: "8.0.0",
    maximumVersion: "9.*.*",
    integrationName: "ILogger",
    type: InstrumentationType.Log)]
[Browsable(false)]
[EditorBrowsable(EditorBrowsableState.Never)]
public class ExtendedLoggerFactoryConstructorIntegration
{
    internal static CallTargetState OnMethodBegin<TTarget, TProviders, TEnrichers, TStaticEnrichers, TFilterOptions, TFactoryOptions, TScopeProvider, TEnrichmentOptions, TRedactionOptions, TRedactorProvider>(TTarget instance, TProviders providers, TEnrichers enrichers, TStaticEnrichers staticEnrichers, TFilterOptions filterOptions, TFactoryOptions factoryOptions, TScopeProvider scopeProvider, TEnrichmentOptions enrichmentOptions, TRedactionOptions redactionOptions, TRedactorProvider redactorProvider)
    {
        return new CallTargetState(null, state: scopeProvider);
    }

    internal static CallTargetReturn OnMethodEnd<TTarget>(TTarget instance, Exception? exception, in CallTargetState state)
    {
        // if (/* isDisabled */ false)
        // {
        //     return CallTargetReturn.GetDefault();
        // }

        if (exception is not null)
        {
            // If there's an exception during the constructor, things aren't going to work anyway
            return CallTargetReturn.GetDefault();
        }

        var scopeProvider = state.State is { } rawScopeProvider
                                ? rawScopeProvider.DuckCast<IExternalScopeProvider>()
                                : instance!.DuckCast<ExtendedLoggerFactoryProxy>().ScopeProvider;

        LoggerFactoryIntegrationCommon<TTarget>.TryAddDirectSubmissionLoggerProvider(instance, scopeProvider);

        return CallTargetReturn.GetDefault();
    }
}

#endif
