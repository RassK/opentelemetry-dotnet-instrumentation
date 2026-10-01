// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.OpAmp.Client.Listeners;
using OpenTelemetry.OpAmp.Client.Messages;

namespace OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member - TODO
#pragma warning disable SA1600 // Missing XML comment for publicly visible type or member - TODO
#pragma warning disable RS0016 // TODO

public interface IOpAmpClient
{
    void Subscribe<T>(IOpAmpListener<T> listener)
        where T : OpAmpMessage;

    void Unsubscribe<T>(IOpAmpListener<T> listener)
        where T : OpAmpMessage;

    void ReportCustomCapabilities(IEnumerable<string> capabilities);

    void SendRemoteConfigStatus(RemoteConfigStatusReport report);

    void SendCustomMessage(string capability, string type, ReadOnlyMemory<byte> data);
}
