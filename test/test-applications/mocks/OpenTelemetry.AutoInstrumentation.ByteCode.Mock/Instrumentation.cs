// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Bridge;

namespace OpenTelemetry.AutoInstrumentation.ByteCode;

#pragma warning disable SA1600 // Elements should be documented

public static class Instrumentation
{
    public static void Initialize(ICommonBridge commonBridge)
    {
        Console.WriteLine($"{nameof(Initialize)} called");
    }
}
