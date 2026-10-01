// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.OpAmp.Client.Listeners;
using OpenTelemetry.OpAmp.Client.Messages;

namespace OpenTelemetry.AutoInstrumentation.OpAmp.Listeners;

internal class FlagsMessageListener : IOpAmpListener<FlagsMessage>
{
    private readonly OpAmpManager _manager;

    public FlagsMessageListener(OpAmpManager manager)
    {
        _manager = manager;
    }

    public void HandleMessage(FlagsMessage message)
    {
        if (message.Flags == ServerSentFlags.ReportFullState)
        {
            _manager.SendFullStateReport();
        }
    }
}
