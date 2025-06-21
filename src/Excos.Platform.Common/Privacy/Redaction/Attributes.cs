// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

namespace Excos.Platform.Common.Privacy.Redaction;

public abstract class ValueHashedAttribute : Attribute
{
}

/// <summary>
/// When applied to a property, field, or parameter, indicates that the value should be hashed before being logged.
/// User specific hashing salt should be used in the process.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
public sealed class UserHashedAttribute : ValueHashedAttribute
{
	public UserHashedAttribute()
	{
	}
}

/// <summary>
/// When applied to a property, field, or parameter, indicates that the value should be hashed before being logged.
/// Tenant specific hashing salt should be used in the process.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
public sealed class TenantHashedAttribute : ValueHashedAttribute
{
	public TenantHashedAttribute()
	{
	}
}