// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

using System.ComponentModel;
using System.Reflection;
using JasperFx.Core.Reflection;
using Microsoft.Extensions.Compliance.Classification;

namespace Excos.Platform.Common.Privacy.Redaction;

public record class PrivacyValueDescriptor(MemberInfo Source, PrivacyValueRedaction Redaction)
{
	private static readonly Dictionary<MemberInfo, string> MemberDisplayNameCache = new();
	private static readonly Dictionary<Type, string> TypeDisplayNameCache = new();
	private static readonly Dictionary<Type, List<PrivacyValueDescriptor>> DescriptorCache = new();

	public string OpenTelemetryName => GetDisplayName(this.Source);

	public static PrivacyValueDescriptor? CreateOrNull(MemberInfo source)
	{
		ValueHashedAttribute? valueHashedAttribute = source.GetCustomAttribute<ValueHashedAttribute>();
		DataClassificationAttribute? dataClassificationAttribute = source.GetCustomAttribute<DataClassificationAttribute>();

		if (valueHashedAttribute == null && dataClassificationAttribute == null)
			return null;

		switch (dataClassificationAttribute?.Classification.Value)
		{
			// We should not log sensitive values
			case ClassificationConstants.UII:
			case ClassificationConstants.CC:
				return null;
			case ClassificationConstants.UPI:
			case ClassificationConstants.OI:
			case ClassificationConstants.UDI:
			case ClassificationConstants.SYS:
				if (valueHashedAttribute is UserHashedAttribute)
					return new PrivacyValueDescriptor(source, PrivacyValueRedaction.UserHashed);
				if (valueHashedAttribute is TenantHashedAttribute)
					return new PrivacyValueDescriptor(source, PrivacyValueRedaction.TenantHashed);
				return new PrivacyValueDescriptor(source, PrivacyValueRedaction.None);
			default:
				return null; // do not log unclassified data
		}
	}

	public static string GetDisplayName(MemberInfo source)
	{
		if (MemberDisplayNameCache.TryGetValue(source, out string? displayName))
		{
			return displayName;
		}

		displayName = source.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? $"{source.Name}";
		MemberDisplayNameCache[source] = displayName;
		return displayName;
	}

	public static string GetDisplayName(Type type)
	{
		if (TypeDisplayNameCache.TryGetValue(type, out string? displayName))
		{
			return displayName;
		}

		displayName = type.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? type.FullNameInCode();
		TypeDisplayNameCache[type] = displayName;
		return displayName;
	}

	public static List<PrivacyValueDescriptor> GetDescriptors(Type type)
	{
		if (DescriptorCache.TryGetValue(type, out List<PrivacyValueDescriptor>? descriptors))
		{
			return descriptors;
		}

		descriptors = new List<PrivacyValueDescriptor>();
		foreach (MemberInfo member in type.GetMembers())
		{
			PrivacyValueDescriptor? descriptor = CreateOrNull(member);
			if (descriptor != null)
				descriptors.Add(descriptor);
		}

		DescriptorCache[type] = descriptors;
		return descriptors;
	}

	public string GetValue(object evnt)
	{
		switch (this.Source)
		{
			case PropertyInfo propertyInfo:
				return propertyInfo.GetValue(evnt)?.ToString() ?? string.Empty;
			case FieldInfo fieldInfo:
				return fieldInfo.GetValue(evnt)?.ToString() ?? string.Empty;
			default:
				return string.Empty;
		}
	}
}