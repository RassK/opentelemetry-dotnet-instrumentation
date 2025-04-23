// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using OpenTelemetry.AutoInstrumentation.ByteCode;
using OpenTelemetry.AutoInstrumentation.Configurations;

namespace OpenTelemetry.AutoInstrumentation.Plugins;

internal partial class PluginManager
{
    private readonly IReadOnlyList<(Type Type, object Instance)> _plugins;

    public PluginManager(GeneralSettings settings)
    {
        var plugins = new List<(Type, object)>();

        foreach (var assemblyQualifiedName in settings.Plugins)
        {
            var type = Type.GetType(assemblyQualifiedName, throwOnError: true)!;
            var instance = Activator.CreateInstance(type)!;

            plugins.Add((type, instance));
        }

        _plugins = plugins;
    }

    // Created for testing purposes.
    internal IReadOnlyList<(Type Type, object Instance)> Plugins => _plugins;

    public void Initializing()
    {
        CallPlugins("Initializing");
    }

    /// <summary>
    /// This functionality is used for testing purposes. GetAllDefinitionsPayload is not documented on the plugins.md.
    /// The contacts is based on internal classes and InternalsVisibleTo attributes. It should be refactored before supporting it.
    /// </summary>
    /// <returns>List of payloads.</returns>
    public IEnumerable<InstrumentationDefinitions.Payload> GetAllDefinitionsPayloads()
    {
        var payloads = new List<InstrumentationDefinitions.Payload>();

        foreach (var plugin in _plugins)
        {
            var mi = plugin.Type.GetMethod("GetAllDefinitionsPayload", BindingFlags.NonPublic | BindingFlags.Instance);
            if (mi is not null)
            {
                if (mi.Invoke(plugin.Instance, null) is InstrumentationDefinitions.Payload payload)
                {
                    payloads.Add(payload);
                }
            }
        }

        return payloads;
    }

    public void ConfigureTracesOptions<T>(T options)
        where T : notnull
    {
        CallPlugins("ConfigureTracesOptions", (typeof(T), options));
    }

    private T ConfigureBuilder<T>(T builder, string methodName)
        where T : notnull
    {
        CallPlugins(methodName, (typeof(T), builder));

        return builder;
    }

    private void CallPlugins(string methodName)
    {
        foreach (var plugin in _plugins)
        {
            var mi = plugin.Type.GetMethod(methodName, Type.EmptyTypes);
            if (mi is not null)
            {
                mi.Invoke(plugin.Instance, null);
            }
        }
    }

    private void CallPlugins(string methodName, (Type Type, object Value) arg)
    {
        foreach (var plugin in _plugins)
        {
            var mi = plugin.Type.GetMethod(methodName, new[] { arg.Type });
            if (mi is not null)
            {
                mi.Invoke(plugin.Instance, new object[] { arg.Value });
            }
        }
    }
}
