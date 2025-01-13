// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.DuckTyping;
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace OpenTelemetry.AutoInstrumentation.ByteCode.Instrumentations.MongoDB.DuckTypes;

/// <summary>
/// MongoDB.Bson.BsonDocument interface for duck-typing
/// </summary>
[DuckCopy]
internal struct BsonElementStruct
{
    /// <summary>
    /// Gets the name of the element.
    /// </summary>
    public string Name;

    /// <summary>
    /// Gets the value of the element.
    /// </summary>
    public object? Value;
}
