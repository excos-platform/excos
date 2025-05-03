﻿using System;
using System.Collections.Generic;
using System.Linq;
using LimeFlight.OpenAPI.Diff.BusinessObjects;
using LimeFlight.OpenAPI.Diff.Extensions;
using LimeFlight.OpenAPI.Diff.Utils;
using Microsoft.OpenApi.Models;

namespace LimeFlight.OpenAPI.Diff.Compare
{
	public class SecurityRequirementDiff
	{
		private readonly OpenApiComponents _leftComponents;
		private readonly OpenApiDiff _openApiDiff;
		private readonly OpenApiComponents _rightComponents;

		public SecurityRequirementDiff(OpenApiDiff openApiDiff)
		{
			this._openApiDiff = openApiDiff;
			this._leftComponents = openApiDiff.OldSpecOpenApi?.Components;
			this._rightComponents = openApiDiff.NewSpecOpenApi?.Components;
		}

		public static OpenApiSecurityRequirement GetCopy(Dictionary<OpenApiSecurityScheme, IList<string>> right)
		{
			var newSecurityRequirement = new OpenApiSecurityRequirement();
			foreach ((OpenApiSecurityScheme key, IList<string> value) in right) newSecurityRequirement.Add(key, value);

			return newSecurityRequirement;
		}

		private OpenApiSecurityRequirement Contains(
			OpenApiSecurityRequirement right, string schemeRef)
		{
			OpenApiSecurityScheme leftSecurityScheme = this._leftComponents.SecuritySchemes[schemeRef];
			var found = new OpenApiSecurityRequirement();

			foreach (KeyValuePair<OpenApiSecurityScheme, IList<string>> keyValuePair in right)
			{
				OpenApiSecurityScheme rightSecurityScheme = this._rightComponents.SecuritySchemes[keyValuePair.Key.Reference?.ReferenceV3];
				if (leftSecurityScheme.Type == rightSecurityScheme.Type)
					switch (leftSecurityScheme.Type)
					{
						case SecuritySchemeType.ApiKey:
							if (leftSecurityScheme.Name == rightSecurityScheme.Name)
							{
								found.Add(keyValuePair.Key, keyValuePair.Value);
								return found;
							}

							break;
						case SecuritySchemeType.Http:
						case SecuritySchemeType.OAuth2:
						case SecuritySchemeType.OpenIdConnect:
							found.Add(keyValuePair.Key, keyValuePair.Value);
							return found;
						default:
							throw new ArgumentOutOfRangeException();
					}
			}

			return found;
		}

		public ChangedSecurityRequirementBO Diff(
			OpenApiSecurityRequirement left, OpenApiSecurityRequirement right, DiffContextBO context)
		{
			var changedSecurityRequirement =
				new ChangedSecurityRequirementBO(left, right != null ? GetCopy(right) : null);

			left ??= new OpenApiSecurityRequirement();
			right ??= new OpenApiSecurityRequirement();

			foreach ((OpenApiSecurityScheme key, IList<string> value) in left)
			{
				OpenApiSecurityRequirement rightSec = this.Contains(right, key.Reference?.ReferenceV3);
				if (rightSec.IsNullOrEmpty())
				{
					changedSecurityRequirement.Missing.Add(key, value);
				}
				else
				{
					OpenApiSecurityScheme rightSchemeRef = rightSec.Keys.First();
					right.Remove(rightSchemeRef);
					ChangedSecuritySchemeBO diff =
						this._openApiDiff
							.SecuritySchemeDiff
							.Diff(
								key.Reference?.ReferenceV3,
								value.ToList(),
								rightSchemeRef.Reference?.ReferenceV3,
								rightSec[rightSchemeRef].ToList(),
								context);
					if (diff != null)
						changedSecurityRequirement.Changed.Add(diff);
				}
			}

			foreach ((OpenApiSecurityScheme key, IList<string> value) in right) changedSecurityRequirement.Increased.Add(key, value);

			return ChangedUtils.IsChanged(changedSecurityRequirement);
		}
	}
}