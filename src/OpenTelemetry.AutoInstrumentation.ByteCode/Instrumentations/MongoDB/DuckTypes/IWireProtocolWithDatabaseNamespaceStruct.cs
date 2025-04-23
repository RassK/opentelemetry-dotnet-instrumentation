// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.DuckTyping;
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace OpenTelemetry.AutoInstrumentation.ByteCode.Instrumentations.MongoDB.DuckTypes;

/// <summary>
/// MongoDB.Driver.Core.IWireProtocol interface for duck-typing
/// </summary>
[DuckCopy]
internal struct IWireProtocolWithDatabaseNamespaceStruct
{
    /// <summary>
    /// Gets the database namespace object passed into the wire protocol
    /// </summary>
    [DuckField(Name = "_databaseNamespace")]
    public object? DatabaseNamespace;
}
