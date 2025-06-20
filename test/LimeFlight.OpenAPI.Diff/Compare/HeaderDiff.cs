﻿using System.Collections.Generic;
using LimeFlight.OpenAPI.Diff.BusinessObjects;
using LimeFlight.OpenAPI.Diff.Enums;
using LimeFlight.OpenAPI.Diff.Utils;
using Microsoft.OpenApi.Models;

namespace LimeFlight.OpenAPI.Diff.Compare
{
	public class HeaderDiff : ReferenceDiffCache<OpenApiHeader, ChangedHeaderBO>
	{
		private static readonly RefPointer<OpenApiHeader> RefPointer =
			new RefPointer<OpenApiHeader>(RefTypeEnum.Headers);

		private readonly OpenApiComponents _leftComponents;
		private readonly OpenApiDiff _openApiDiff;
		private readonly OpenApiComponents _rightComponents;

		public HeaderDiff(OpenApiDiff openApiDiff)
		{
			this._openApiDiff = openApiDiff;
			this._leftComponents = openApiDiff.OldSpecOpenApi?.Components;
			this._rightComponents = openApiDiff.NewSpecOpenApi?.Components;
		}

		public ChangedHeaderBO Diff(OpenApiHeader left, OpenApiHeader right, DiffContextBO context)
		{
			return this.CachedDiff(left, right, left.Reference?.ReferenceV3,
				right.Reference?.ReferenceV3, context);
		}

		protected override ChangedHeaderBO ComputeDiff(OpenApiHeader left, OpenApiHeader right,
			DiffContextBO context)
		{
			left = RefPointer.ResolveRef(this._leftComponents, left, left.Reference?.ReferenceV3);
			right = RefPointer.ResolveRef(this._rightComponents, right, right.Reference?.ReferenceV3);

			var changedHeader =
				new ChangedHeaderBO(left, right, context)
				{
					Required = GetBooleanDiff(left.Required, right.Required),
					Deprecated = !left.Deprecated && right.Deprecated,
					Style = left.Style != right.Style,
					Explode = GetBooleanDiff(left.Explode, right.Explode),
					Description = this._openApiDiff
						.MetadataDiff
						.Diff(left.Description, right.Description, context),
					Schema = this._openApiDiff
						.SchemaDiff
						.Diff(left.Schema, right.Schema, context.CopyWithRequired(true)),
					Content = this._openApiDiff
						.ContentDiff
						.Diff(left.Content, right.Content, context),
					Extensions = this._openApiDiff
						.ExtensionsDiff
						.Diff(left.Extensions, right.Extensions, context)
				};

			return ChangedUtils.IsChanged(changedHeader);
		}

		private static bool GetBooleanDiff(bool? left, bool? right)
		{
			bool leftRequired = left ?? false;
			bool rightRequired = right ?? false;
			return leftRequired != rightRequired;
		}
	}
}