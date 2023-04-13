// <copyright file="InstrumentationAssemblyRule.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System.Diagnostics;
using System.Reflection;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.RulesEngine;

internal partial class InstrumentationAssemblyRule : Rule
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger("StartupHook");

    public InstrumentationAssemblyRule()
    {
        Name = "Instrumentation Assembly Validator";
        Description = "Ensure that the version of the OpenTelemetry Instrumentation libraries is not older than the version used by Auto-Instrumentation.";
    }

    internal override bool Evaluate()
    {
        var result = true;

        try
        {
            var ruleFileInfoList = RuleEngineMetadata.GetFileInfos();
            var entryAssembly = Assembly.GetEntryAssembly();
            var referencedAssemblies = entryAssembly?.GetReferencedAssemblies();

            if (referencedAssemblies == null)
            {
                Logger.Warning($"Rule Engine: Could not get referenced assembly (GetReferencedAssemblies()) from an application. Skipping rule evaluation.");
                return result;
            }

            foreach (var referencedAssembly in referencedAssemblies)
            {
                var ruleFileInfo = ruleFileInfoList.FirstOrDefault(file => file.FileName == referencedAssembly.Name);
                if (ruleFileInfo != null)
                {
                    var autoInstrumentationFileVersion = new Version(ruleFileInfo.FileVersion);

                    var appInstrumentationAssembly = Assembly.Load(referencedAssembly);
                    var appInstrumentationFileVersionInfo = FileVersionInfo.GetVersionInfo(appInstrumentationAssembly.Location);
                    var appInstrumentationFileVersion = new Version(appInstrumentationFileVersionInfo.FileVersion);

                    if (appInstrumentationFileVersion < autoInstrumentationFileVersion)
                    {
                        result = false;
                        Logger.Error($"Rule Engine: Application has direct or indirect reference to older version of Instrumentation library {ruleFileInfo.FileName} - {ruleFileInfo.FileVersion}.");
                    }
                    else
                    {
                        Logger.Information($"Rule Engine: Application has reference to Instrumentation library {ruleFileInfo.FileName} and loaded successfully.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Exception in rule evaluation should not impact the result of the rule.
            Logger.Warning($"Rule Engine:Couldn't evaluate OpenTelemetry Instrumentation Evaluation. Exception: {ex}");
        }

        return true;
    }
}
