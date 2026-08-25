// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.OpAmp.Client.Listeners;
using OpenTelemetry.OpAmp.Client.Messages;

namespace OpenTelemetry.AutoInstrumentation.OpAmp.Listeners;

internal class CapabilitiesListener :
    IOpAmpListener<ServerCapabilitiesMessage>,
    IOpAmpListener<CustomCapabilitiesMessage>
{
    private readonly OpAmpManager _manager;

    public CapabilitiesListener(OpAmpManager manager)
    {
        _manager = manager;
    }

    public void HandleMessage(ServerCapabilitiesMessage message)
    {
        _manager.UpdateServerCapabilities(message.Capabilities);
    }

    public void HandleMessage(CustomCapabilitiesMessage message)
    {
        _manager.UpdateServerCustomCapabilities(message.Capabilities);
    }
}
