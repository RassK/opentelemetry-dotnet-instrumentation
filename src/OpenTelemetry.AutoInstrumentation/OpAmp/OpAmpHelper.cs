// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using OpenTelemetry.AutoInstrumentation.Util;
using OpenTelemetry.OpAmp.Client;
using OpenTelemetry.OpAmp.Client.Settings;

namespace OpenTelemetry.AutoInstrumentation.OpAmp;

internal static class OpAmpHelper
{
    public static string GetOpAmpVersion()
    {
        var assembly = typeof(OpAmpClient).Assembly;

        return assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion?.Split(['+'], 2)[0] ?? "unknown";
    }

    public static ConnectionType GetConnectionType(Uri serverUrl)
    {
        if (serverUrl.Scheme == UriSchemes.Http ||
            serverUrl.Scheme == UriSchemes.Https)
        {
            return ConnectionType.Http;
        }

        if (serverUrl.Scheme == UriSchemes.Ws ||
            serverUrl.Scheme == UriSchemes.Wss)
        {
            return ConnectionType.WebSocket;
        }

        throw new NotSupportedException($"Connection type '{serverUrl.Scheme}' is not supported.");
    }

    public static void AddAttribute(this IdentificationSettings settings, string key, object value)
    {
        void AddTypedAttribute<T>(T val, Action<string, T> identifying, Action<string, T> nonIdentifying)
        {
            if (IsIdentifyingAttribute(key))
            {
                identifying(key, val);
            }
            else
            {
                nonIdentifying(key, val);
            }
        }

        switch (value)
        {
            case string s:
                AddTypedAttribute(s, settings.AddIdentifyingAttribute, settings.AddNonIdentifyingAttribute);
                break;
            case int i:
                AddTypedAttribute(i, settings.AddIdentifyingAttribute, settings.AddNonIdentifyingAttribute);
                break;
            case double d:
                AddTypedAttribute(d, settings.AddIdentifyingAttribute, settings.AddNonIdentifyingAttribute);
                break;
            case bool b:
                AddTypedAttribute(b, settings.AddIdentifyingAttribute, settings.AddNonIdentifyingAttribute);
                break;
            default:
                var stringValue = value.ToString();
                if (!string.IsNullOrWhiteSpace(stringValue))
                {
                    AddTypedAttribute(stringValue, settings.AddIdentifyingAttribute, settings.AddNonIdentifyingAttribute);
                }

                break;
        }
    }

    private static bool IsIdentifyingAttribute(string attributeName)
    {
        return attributeName
            is Constants.ResourceAttributes.AttributeServiceName
            or Constants.ResourceAttributes.AttributeServiceInstanceId
            or Constants.ResourceAttributes.AttributeServiceNamespaceName;
    }
}
