// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;
using OpenTelemetry.OpAmp.Client.Listeners;
using OpenTelemetry.OpAmp.Client.Messages;

namespace OpenTelemetry.AutoInstrumentation.OpAmp;

internal class PluginOpAmpClient : IOpAmpClient
{
    private OpAmpManager _manager;

    public PluginOpAmpClient(OpAmpManager manager)
    {
        _manager = manager;
    }

    public void ReportCustomCapabilities(IEnumerable<string> capabilities)
    {
        _manager.UpdateClientCustomCapabilities(capabilities);
    }

    public void SendCustomMessage(string capability, string type, ReadOnlyMemory<byte> data)
    {
        _manager.SendCustomMessage(capability, type, data);
    }

    public void SendRemoteConfigStatus(RemoteConfigStatusReport report)
    {
        _manager.SendRemoteConfigStatus(report);
    }

    public void Subscribe<T>(IOpAmpListener<T> listener)
        where T : OpAmpMessage
    {
        _manager.Subscribe(listener);
    }

    public void Unsubscribe<T>(IOpAmpListener<T> listener)
        where T : OpAmpMessage
    {
        _manager.Unsubscribe(listener);
    }
}
