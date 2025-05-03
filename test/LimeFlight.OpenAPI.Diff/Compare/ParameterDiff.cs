using System.Collections.Generic;
using LimeFlight.OpenAPI.Diff.BusinessObjects;
using LimeFlight.OpenAPI.Diff.Enums;
using LimeFlight.OpenAPI.Diff.Utils;
using Microsoft.OpenApi.Models;

namespace LimeFlight.OpenAPI.Diff.Compare
{
	public class ParameterDiff : ReferenceDiffCache<OpenApiParameter, ChangedParameterBO>
	{
		private static readonly RefPointer<OpenApiParameter> RefPointer =
			new RefPointer<OpenApiParameter>(RefTypeEnum.Parameters);

		private readonly OpenApiComponents _leftComponents;
		private readonly OpenApiDiff _openApiDiff;
		private readonly OpenApiComponents _rightComponents;

		public ParameterDiff(OpenApiDiff openApiDiff)
		{
			this._openApiDiff = openApiDiff;
			this._leftComponents = openApiDiff.OldSpecOpenApi?.Components;
			this._rightComponents = openApiDiff.NewSpecOpenApi?.Components;
		}

		public ChangedParameterBO Diff(OpenApiParameter left, OpenApiParameter right, DiffContextBO context)
		{
			return this.CachedDiff(left, right, left.Reference?.ReferenceV3,
				right.Reference?.ReferenceV3, context);
		}

		protected override ChangedParameterBO ComputeDiff(OpenApiParameter left,
			OpenApiParameter right, DiffContextBO context)
		{
			left = RefPointer.ResolveRef(this._leftComponents, left, left.Reference?.ReferenceV3);
			right = RefPointer.ResolveRef(this._rightComponents, right, right.Reference?.ReferenceV3);

			var changedParameter =
				new ChangedParameterBO(right.Name, right.In, left, right, context)
				{
					IsChangeRequired = GetBooleanDiff(left.Required, right.Required),
					IsDeprecated = !left.Deprecated && right.Deprecated,
					ChangeAllowEmptyValue = GetBooleanDiff(left.AllowEmptyValue, right.AllowEmptyValue),
					ChangeStyle = left.Style != right.Style,
					ChangeExplode = GetBooleanDiff(left.Explode, right.Explode),
					Schema = this._openApiDiff
						.SchemaDiff
						.Diff(left.Schema, right.Schema, context.CopyWithRequired(true)),
					Description = this._openApiDiff
						.MetadataDiff
						.Diff(left.Description, right.Description, context),
					Content = this._openApiDiff
						.ContentDiff
						.Diff(left.Content, right.Content, context),
					Extensions = this._openApiDiff
						.ExtensionsDiff
						.Diff(left.Extensions, right.Extensions, context)
				};

			return ChangedUtils.IsChanged(changedParameter);
		}

		private static bool GetBooleanDiff(bool? left, bool? right)
		{
			var leftRequired = left ?? false;
			var rightRequired = right ?? false;
			return leftRequired != rightRequired;
		}
	}
}