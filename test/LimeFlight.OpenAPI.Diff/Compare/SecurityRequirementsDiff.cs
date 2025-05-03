﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using LimeFlight.OpenAPI.Diff.BusinessObjects;
using LimeFlight.OpenAPI.Diff.Enums;
using LimeFlight.OpenAPI.Diff.Utils;
using Microsoft.OpenApi.Models;

namespace LimeFlight.OpenAPI.Diff.Compare
{
	public class SecurityRequirementsDiff
	{
		private static RefPointer<OpenApiSecurityScheme> _refPointer =
			new RefPointer<OpenApiSecurityScheme>(RefTypeEnum.SecuritySchemes);

		private readonly OpenApiComponents _leftComponents;
		private readonly OpenApiDiff _openApiDiff;
		private readonly OpenApiComponents _rightComponents;

		public SecurityRequirementsDiff(OpenApiDiff openApiDiff)
		{
			this._openApiDiff = openApiDiff;
			this._leftComponents = openApiDiff.OldSpecOpenApi?.Components;
			this._rightComponents = openApiDiff.NewSpecOpenApi?.Components;
		}

		public OpenApiSecurityRequirement Contains(IList<OpenApiSecurityRequirement> securityRequirements,
			OpenApiSecurityRequirement left)
		{
			return securityRequirements
				.FirstOrDefault(x => this.Same(left, x));
		}

		public bool Same(OpenApiSecurityRequirement left, OpenApiSecurityRequirement right)
		{
			var leftTypes = GetListOfSecuritySchemes(this._leftComponents, left);
			var rightTypes = GetListOfSecuritySchemes(this._rightComponents, right);

			return leftTypes.SequenceEqual(rightTypes);
		}

		private static ImmutableDictionary<SecuritySchemeType, ParameterLocation> GetListOfSecuritySchemes(
			OpenApiComponents components, OpenApiSecurityRequirement securityRequirement)
		{
			var tmpResult = new Dictionary<SecuritySchemeType, ParameterLocation>();
			foreach (var openApiSecurityScheme in securityRequirement.Keys.ToList())
				if (components.SecuritySchemes.TryGetValue(openApiSecurityScheme.Reference?.ReferenceV3,
					out var result))
				{
					if (!tmpResult.ContainsKey(result.Type))
						tmpResult.Add(result.Type, result.In);
				}
				else
				{
					throw new ArgumentException("Impossible to find security scheme: " + openApiSecurityScheme.Scheme);
				}

			return tmpResult.ToImmutableDictionary();
		}

		public ChangedSecurityRequirementsBO Diff(
			IList<OpenApiSecurityRequirement> left, IList<OpenApiSecurityRequirement> right, DiffContextBO context)
		{
			left ??= new List<OpenApiSecurityRequirement>();
			right = right != null ? GetCopy(right) : new List<OpenApiSecurityRequirement>();

			var changedSecurityRequirements = new ChangedSecurityRequirementsBO(left, right);

			foreach (var leftSecurity in left)
			{
				var rightSecOpt = this.Contains(right, leftSecurity);
				if (rightSecOpt == null)
				{
					changedSecurityRequirements.Missing.Add(leftSecurity);
				}
				else
				{
					var rightSec = rightSecOpt;

					right.Remove(rightSec);
					var diff =
						this._openApiDiff.SecurityRequirementDiff
							.Diff(leftSecurity, rightSec, context);
					if (diff != null)
						changedSecurityRequirements.Changed.Add(diff);
				}
			}

			changedSecurityRequirements.Increased.AddRange(right);

			return ChangedUtils.IsChanged(changedSecurityRequirements);
		}

		private static List<OpenApiSecurityRequirement> GetCopy(IEnumerable<OpenApiSecurityRequirement> right)
		{
			return right.Select(SecurityRequirementDiff.GetCopy).ToList();
		}
	}
}