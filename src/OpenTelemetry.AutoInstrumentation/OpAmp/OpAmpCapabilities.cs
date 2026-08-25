// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.OpAmp;

[Flags]
internal enum OpAmpCapabilities
{
    None = 0,

    ReportsEffectiveConfig = 1,

    ReportsRemoteConfigStatus = 2
}
