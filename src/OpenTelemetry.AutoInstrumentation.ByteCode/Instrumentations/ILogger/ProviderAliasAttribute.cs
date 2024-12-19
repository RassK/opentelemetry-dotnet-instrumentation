// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET

namespace Microsoft.Extensions.Logging;

/// <summary>
/// Defines alias for ILoggerProvider implementation to be used in filtering rules.
/// Based on https://github.com/dotnet/runtime/blob/793676e851989ac81291b194d5efbcd5e800b673/src/libraries/Microsoft.Extensions.Logging/src/ProviderAliasAttribute.cs
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
internal class ProviderAliasAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderAliasAttribute"/> class.
    /// </summary>
    /// <param name="alias">The alias to set.</param>
    public ProviderAliasAttribute(string alias)
    {
        Alias = alias;
    }

    /// <summary>
    /// Gets the alias of the provider.
    /// </summary>
    public string Alias { get; }
}

#endif
