// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.ByteCode;

public static class Instrumentation
{
    public static void Initialize()
    {
        Console.WriteLine($"{nameof(Initialize)} called");
    }
}
