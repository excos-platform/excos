// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

using Excos.Platform.Common.Utils;

namespace Excos.Platform.Common.Privacy.Redaction;

public class PrivacyValueRedactor
{
	// TODO: read the salt from user/tenant profile
	private static readonly Guid TempNamespace = new("898a657a-598f-4254-912f-053889bb8d80");
	public string Redact(string value, PrivacyValueRedaction redaction)
	{
		switch (redaction)
		{
			case PrivacyValueRedaction.None:
				return value;
			case PrivacyValueRedaction.UserHashed:
				return TempNamespace.V5(value).ToString();
			case PrivacyValueRedaction.TenantHashed:
				return TempNamespace.V5(value).ToString();
			default:
				throw new ArgumentOutOfRangeException(nameof(redaction), redaction, null);
		}
	}
}