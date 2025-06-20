// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

using Microsoft.Extensions.Compliance.Classification;

namespace Excos.Platform.Common.Privacy;

internal static class ClassificationConstants
{
	// For details see: https://devblog.dziubiak.pl/excos/03-privacy-compliance/
	public const string TaxonomyName = "ExcosPrivacyTaxonomy";

	public const string UII = "User Identifiable Information";
	public const string UPI = "User Pseudonymous Identifier";
	public const string UDI = "User Demographic Information";
	public const string CC = "Customer Content";
	public const string OI = "Organization Information";
	public const string SYS = "System Metadata";
}

/// <summary>
/// User Identifiable Information
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
public sealed class UIIAttribute : DataClassificationAttribute
{
	public UIIAttribute() : base(new DataClassification(ClassificationConstants.TaxonomyName, ClassificationConstants.UII))
	{
	}
}

/// <summary>
/// User Pseudonymous Identifier
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
public sealed class UPIAttribute : DataClassificationAttribute
{
	public UPIAttribute() : base(new DataClassification(ClassificationConstants.TaxonomyName, ClassificationConstants.UPI))
	{
	}
}

/// <summary>
/// User Demographic Information
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
public sealed class UDIAttribute : DataClassificationAttribute
{
	public UDIAttribute() : base(new DataClassification(ClassificationConstants.TaxonomyName, ClassificationConstants.UDI))
	{
	}
}

/// <summary>
/// Customer Content
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
public sealed class CCAttribute : DataClassificationAttribute
{
	public CCAttribute() : base(new DataClassification(ClassificationConstants.TaxonomyName, ClassificationConstants.CC))
	{
	}
}

/// <summary>
/// Organization Information
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
public sealed class OIAttribute : DataClassificationAttribute
{
	public OIAttribute() : base(new DataClassification(ClassificationConstants.TaxonomyName, ClassificationConstants.OI))
	{
	}
}

/// <summary>
/// System Metadata
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
public sealed class SYSAttribute : DataClassificationAttribute
{
	public SYSAttribute() : base(new DataClassification(ClassificationConstants.TaxonomyName, ClassificationConstants.SYS))
	{
	}
}