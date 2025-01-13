// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.DuckTyping;
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace OpenTelemetry.AutoInstrumentation.ByteCode.Instrumentations.MongoDB.DuckTypes;

/// <summary>
/// MongoDB.Driver.DatabaseNamespace interface for duck-typing
/// </summary>
[DuckCopy]
internal struct DatabaseNamespaceStruct
{
    /// <summary>
    /// Gets the name of the database
    /// </summary>
    public string? DatabaseName;
}
