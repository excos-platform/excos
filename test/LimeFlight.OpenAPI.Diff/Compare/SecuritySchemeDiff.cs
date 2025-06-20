﻿using System;
using System.Collections.Generic;
using LimeFlight.OpenAPI.Diff.BusinessObjects;
using LimeFlight.OpenAPI.Diff.Utils;
using Microsoft.OpenApi.Models;

namespace LimeFlight.OpenAPI.Diff.Compare
{
	public class SecuritySchemeDiff : ReferenceDiffCache<OpenApiSecurityScheme, ChangedSecuritySchemeBO>
	{
		private readonly OpenApiComponents _leftComponents;
		private readonly OpenApiDiff _openApiDiff;
		private readonly OpenApiComponents _rightComponents;

		public SecuritySchemeDiff(OpenApiDiff openApiDiff)
		{
			this._openApiDiff = openApiDiff;
			this._leftComponents = openApiDiff.OldSpecOpenApi?.Components;
			this._rightComponents = openApiDiff.NewSpecOpenApi?.Components;
		}

		public ChangedSecuritySchemeBO Diff(
			string leftSchemeRef,
			List<string> leftScopes,
			string rightSchemeRef,
			List<string> rightScopes,
			DiffContextBO context)
		{
			OpenApiSecurityScheme leftSecurityScheme = this._leftComponents.SecuritySchemes[leftSchemeRef];
			OpenApiSecurityScheme rightSecurityScheme = this._rightComponents.SecuritySchemes[rightSchemeRef];
			ChangedSecuritySchemeBO changedSecuritySchemeOpt =
				this.CachedDiff(
					leftSecurityScheme,
					rightSecurityScheme,
					leftSchemeRef,
					rightSchemeRef,
					context);
			ChangedSecuritySchemeBO changedSecurityScheme =
				changedSecuritySchemeOpt ?? new ChangedSecuritySchemeBO(leftSecurityScheme, rightSecurityScheme);
			changedSecurityScheme = GetCopyWithoutScopes(changedSecurityScheme);

			if (changedSecurityScheme != null
				&& leftSecurityScheme.Type == SecuritySchemeType.OAuth2)
			{
				ChangedSecuritySchemeScopesBO changed = ChangedUtils.IsChanged(ListDiff.Diff(
					new ChangedSecuritySchemeScopesBO(leftScopes, rightScopes)
				));

				if (changed != null)
					changedSecurityScheme.ChangedScopes = changed;
			}

			return ChangedUtils.IsChanged(changedSecurityScheme);
		}

		protected override ChangedSecuritySchemeBO ComputeDiff(
			OpenApiSecurityScheme leftSecurityScheme,
			OpenApiSecurityScheme rightSecurityScheme,
			DiffContextBO context)
		{
			var changedSecurityScheme =
				new ChangedSecuritySchemeBO(leftSecurityScheme, rightSecurityScheme)
				{
					Description = this._openApiDiff
						.MetadataDiff
						.Diff(leftSecurityScheme.Description, rightSecurityScheme.Description, context)
				};

			switch (leftSecurityScheme.Type)
			{
				case SecuritySchemeType.ApiKey:
					changedSecurityScheme.IsChangedIn =
						!leftSecurityScheme.In.Equals(rightSecurityScheme.In);
					break;
				case SecuritySchemeType.Http:
					changedSecurityScheme.IsChangedScheme =
						leftSecurityScheme.Scheme != rightSecurityScheme.Scheme;
					changedSecurityScheme.IsChangedBearerFormat =
						leftSecurityScheme.BearerFormat != rightSecurityScheme.BearerFormat;
					break;
				case SecuritySchemeType.OAuth2:
					changedSecurityScheme.OAuthFlows = this._openApiDiff
						.OAuthFlowsDiff
						.Diff(leftSecurityScheme.Flows, rightSecurityScheme.Flows);
					break;
				case SecuritySchemeType.OpenIdConnect:
					changedSecurityScheme.IsChangedOpenIdConnectUrl =
						leftSecurityScheme.OpenIdConnectUrl != rightSecurityScheme.OpenIdConnectUrl;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			changedSecurityScheme.Extensions = this._openApiDiff
				.ExtensionsDiff
				.Diff(leftSecurityScheme.Extensions, rightSecurityScheme.Extensions, context);

			return changedSecurityScheme;
		}

		private static ChangedSecuritySchemeBO GetCopyWithoutScopes(ChangedSecuritySchemeBO original)
		{
			return new ChangedSecuritySchemeBO(
				original.OldSecurityScheme, original.NewSecurityScheme)
			{
				IsChangedType = original.IsChangedType,
				IsChangedIn = original.IsChangedIn,
				IsChangedScheme = original.IsChangedScheme,
				IsChangedBearerFormat = original.IsChangedBearerFormat,
				Description = original.Description,
				OAuthFlows = original.OAuthFlows,
				IsChangedOpenIdConnectUrl = original.IsChangedOpenIdConnectUrl
			};
		}
	}
}