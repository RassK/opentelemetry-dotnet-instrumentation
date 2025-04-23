// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.ByteCode.Instrumentations.Kafka.DuckTypes;
using OpenTelemetry.AutoInstrumentation.DuckTyping;

namespace OpenTelemetry.AutoInstrumentation.ByteCode.Instrumentations.Kafka.Integrations;

internal static class MessageHeadersHelper<TTypeMarker>
{
    // ReSharper disable once StaticMemberInGenericType
    private static readonly Type HeadersType;

    static MessageHeadersHelper()
    {
        HeadersType = typeof(TTypeMarker).Assembly.GetType("Confluent.Kafka.Headers")!;
    }

    public static IHeaders? Create()
    {
        return Activator.CreateInstance(HeadersType).DuckCast<IHeaders>();
    }
}
