// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0
namespace OpenTelemetry.AutoInstrumentation.ByteCode.Instrumentations.Wcf;

internal interface IKeyedByTypeCollection
{
    void Add(object o);

    bool Contains(Type t);
}
