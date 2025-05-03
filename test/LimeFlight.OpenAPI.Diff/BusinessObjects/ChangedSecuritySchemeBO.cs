using System.Collections.Generic;
using System.Linq;
using LimeFlight.OpenAPI.Diff.Enums;
using LimeFlight.OpenAPI.Diff.Extensions;
using Microsoft.OpenApi.Models;

namespace LimeFlight.OpenAPI.Diff.BusinessObjects
{
	public class ChangedSecuritySchemeBO : ComposedChangedBO
	{
		public ChangedSecuritySchemeBO(OpenApiSecurityScheme oldSecurityScheme, OpenApiSecurityScheme newSecurityScheme)
		{
			this.OldSecurityScheme = oldSecurityScheme;
			this.NewSecurityScheme = newSecurityScheme;
		}

		public OpenApiSecurityScheme OldSecurityScheme { get; }
		public OpenApiSecurityScheme NewSecurityScheme { get; }

		public bool IsChangedType { get; set; }
		public bool IsChangedIn { get; set; }
		public bool IsChangedScheme { get; set; }
		public bool IsChangedBearerFormat { get; set; }
		public bool IsChangedOpenIdConnectUrl { get; set; }
		public ChangedSecuritySchemeScopesBO ChangedScopes { get; set; }
		public ChangedMetadataBO Description { get; set; }
		public ChangedOAuthFlowsBO OAuthFlows { get; set; }
		public ChangedExtensionsBO Extensions { get; set; }
		protected override ChangedElementTypeEnum GetElementType() => ChangedElementTypeEnum.SecurityScheme;

		public override List<(string Identifier, ChangedBO Change)> GetChangedElements()
		{
			return new List<(string Identifier, ChangedBO Change)>
				{
					(null, this.Description),
					(null, this.OAuthFlows),
					(null, this.Extensions)
				}
				.Where(x => x.Change != null).ToList();
		}

		public override DiffResultBO IsCoreChanged()
		{
			if (!this.IsChangedType
				&& !this.IsChangedIn
				&& !this.IsChangedScheme
				&& !this.IsChangedBearerFormat
				&& !this.IsChangedOpenIdConnectUrl
				&& (this.ChangedScopes == null || this.ChangedScopes.IsUnchanged()))
				return new DiffResultBO(DiffResultEnum.NoChanges);
			if (!this.IsChangedType
				&& !this.IsChangedIn
				&& !this.IsChangedScheme
				&& !this.IsChangedBearerFormat
				&& !this.IsChangedOpenIdConnectUrl
				&& (this.ChangedScopes == null || this.ChangedScopes.Increased.IsNullOrEmpty()))
				return new DiffResultBO(DiffResultEnum.Compatible);
			return new DiffResultBO(DiffResultEnum.Incompatible);
		}

		protected override List<ChangedInfoBO> GetCoreChanges()
		{
			var returnList = new List<ChangedInfoBO>();
			ChangedElementTypeEnum elementType = this.GetElementType();
			const TypeEnum changeType = TypeEnum.Changed;

			if (this.IsChangedBearerFormat)
				returnList.Add(new ChangedInfoBO(elementType, changeType, "Bearer Format",
					this.OldSecurityScheme?.BearerFormat, this.NewSecurityScheme?.BearerFormat));

			if (this.IsChangedIn)
				returnList.Add(new ChangedInfoBO(elementType, changeType, "In", this.OldSecurityScheme?.In.ToString(),
					this.NewSecurityScheme?.In.ToString()));

			if (this.IsChangedOpenIdConnectUrl)
				returnList.Add(new ChangedInfoBO(elementType, changeType, "OpenIdConnect Url",
					this.OldSecurityScheme?.OpenIdConnectUrl.ToString(), this.NewSecurityScheme?.OpenIdConnectUrl.ToString()));

			if (this.IsChangedScheme)
				returnList.Add(new ChangedInfoBO(elementType, changeType, "Scheme", this.OldSecurityScheme?.Scheme,
					this.NewSecurityScheme?.Scheme));

			if (this.IsChangedType)
				returnList.Add(new ChangedInfoBO(elementType, changeType, "Type", this.OldSecurityScheme?.Type.ToString(),
					this.NewSecurityScheme?.Type.ToString()));

			return returnList;
		}
	}
}