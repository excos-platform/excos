// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

using System.ComponentModel;
using System.Reflection;
using Excos.Platform.Common.Privacy;
using JasperFx.Core.Reflection;
using Microsoft.Extensions.Compliance.Classification;

namespace Excos.Platform.Common.Privacy.Redaction;

internal record class PrivacyValueDescriptor(MemberInfo Source, PrivacyValueRedaction Redaction)
{
	public string OpenTelemetryName => GetDisplayName(this.Source);

	internal static Dictionary<MemberInfo, string> MemberDisplayNameCache = new();
	internal static string GetDisplayName(MemberInfo source)
	{
		if (MemberDisplayNameCache.TryGetValue(source, out string? displayName))
		{
			return displayName;
		}

		displayName = source.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? $"{source.Name}";
		MemberDisplayNameCache[source] = displayName;
		return displayName;
	}

	internal static Dictionary<Type, string> TypeDisplayNameCache = new();
	internal static string GetDisplayName(Type type)
	{
		if (TypeDisplayNameCache.TryGetValue(type, out string? displayName))
		{
			return displayName;
		}

		displayName = type.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? type.FullNameInCode();
		TypeDisplayNameCache[type] = displayName;
		return displayName;
	}

	internal static PrivacyValueDescriptor? CreateOrNull(MemberInfo source)
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
				if (valueHashedAttribute is UserHashedAttribute)
					return new PrivacyValueDescriptor(source, PrivacyValueRedaction.UserHashed);
				if (valueHashedAttribute is TenantHashedAttribute)
					return new PrivacyValueDescriptor(source, PrivacyValueRedaction.TenantHashed);
				return new PrivacyValueDescriptor(source, PrivacyValueRedaction.None);
			case ClassificationConstants.UDI:
			case ClassificationConstants.SYS:
				return new PrivacyValueDescriptor(source, PrivacyValueRedaction.None);
			default:
				return null; // do not log unclassified data
		}
	}

	internal readonly static Dictionary<Type, List<PrivacyValueDescriptor>> DescriptorCache = new();

	internal static List<PrivacyValueDescriptor> GetDescriptors(Type type)
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

	internal string GetValue(object evnt)
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