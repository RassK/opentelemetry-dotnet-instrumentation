// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET

using System.ComponentModel;
using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.DuckTyping;

namespace OpenTelemetry.AutoInstrumentation.ByteCode.Instrumentations.ILogger;

/// <summary>
/// LoggerFactory() calltarget instrumentation for direct log submission
/// </summary>
[InstrumentMethod(
    assemblyName: "Microsoft.Extensions.Logging",
    typeName: "Microsoft.Extensions.Logging.LoggerFactory",
    methodName: ".ctor",
    returnTypeName: ClrNames.Void,
    parameterTypeNames: new[] { "System.Collections.Generic.IEnumerable`1[Microsoft.Extensions.Logging.ILoggerProvider]", "Microsoft.Extensions.Options.IOptionsMonitor`1[Microsoft.Extensions.Logging.LoggerFilterOptions]", "Microsoft.Extensions.Options.IOptions`1[Microsoft.Extensions.Logging.LoggerFactoryOptions]", "Microsoft.Extensions.Logging.IExternalScopeProvider" },
    minimumVersion: "8.0.0",
    maximumVersion: "9.*.*",
    integrationName: "ILogger",
    type: InstrumentationType.Log)]
[Browsable(false)]
[EditorBrowsable(EditorBrowsableState.Never)]
public class LoggerFactoryConstructorIntegration
{
    internal static CallTargetState OnMethodBegin<TTarget, TProvider, TFilterOptions, TFactoryOptions, TScopeProvider>(TTarget instance, TProvider logProviders, TFilterOptions filterOptions, TFactoryOptions factoryOptions, TScopeProvider scopeProvider)
    {
        return new CallTargetState(null, state: scopeProvider);
    }

    internal static CallTargetReturn OnMethodEnd<TTarget>(TTarget instance, Exception? exception, in CallTargetState state)
    {
        // if (/* isDisabled */ false)
        // {
        //    return CallTargetReturn.GetDefault();
        // }

        if (exception is not null)
        {
            // If there's an exception during the constructor, things aren't going to work anyway
            return CallTargetReturn.GetDefault();
        }

        var scopeProvider = state.State is { } rawScopeProvider
            ? rawScopeProvider.DuckCast<IExternalScopeProvider>()
            : null;
        if (LoggerFactoryIntegrationCommon<TTarget>.TryAddDirectSubmissionLoggerProvider(instance, scopeProvider))
        {
            // TracerManager.Instance.Telemetry.IntegrationGeneratedSpan(IntegrationId.ILogger);
        }

        return CallTargetReturn.GetDefault();
    }
}

#endif
