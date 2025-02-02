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

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
public sealed class UIIAttribute : DataClassificationAttribute
{
	public UIIAttribute() : base(new DataClassification(ClassificationConstants.TaxonomyName, ClassificationConstants.UII))
	{
	}
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
public sealed class UPIAttribute : DataClassificationAttribute
{
	public UPIAttribute() : base(new DataClassification(ClassificationConstants.TaxonomyName, ClassificationConstants.UPI))
	{
	}
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
public sealed class UDIAttribute : DataClassificationAttribute
{
	public UDIAttribute() : base(new DataClassification(ClassificationConstants.TaxonomyName, ClassificationConstants.UDI))
	{
	}
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
public sealed class CCAttribute : DataClassificationAttribute
{
	public CCAttribute() : base(new DataClassification(ClassificationConstants.TaxonomyName, ClassificationConstants.CC))
	{
	}
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
public sealed class OIAttribute : DataClassificationAttribute
{
	public OIAttribute() : base(new DataClassification(ClassificationConstants.TaxonomyName, ClassificationConstants.OI))
	{
	}
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
public sealed class SYSAttribute : DataClassificationAttribute
{
	public SYSAttribute() : base(new DataClassification(ClassificationConstants.TaxonomyName, ClassificationConstants.SYS))
	{
	}
}

public static class ClassificationSet
{
	public static readonly DataClassificationSet UII = new DataClassificationSet(new DataClassification(ClassificationConstants.TaxonomyName, ClassificationConstants.UII));
	public static readonly DataClassificationSet UPI = new DataClassificationSet(new DataClassification(ClassificationConstants.TaxonomyName, ClassificationConstants.UPI));
	public static readonly DataClassificationSet UDI = new DataClassificationSet(new DataClassification(ClassificationConstants.TaxonomyName, ClassificationConstants.UDI));
	public static readonly DataClassificationSet CC = new DataClassificationSet(new DataClassification(ClassificationConstants.TaxonomyName, ClassificationConstants.CC));
	public static readonly DataClassificationSet OI = new DataClassificationSet(new DataClassification(ClassificationConstants.TaxonomyName, ClassificationConstants.OI));
	public static readonly DataClassificationSet SYS = new DataClassificationSet(new DataClassification(ClassificationConstants.TaxonomyName, ClassificationConstants.SYS));
}