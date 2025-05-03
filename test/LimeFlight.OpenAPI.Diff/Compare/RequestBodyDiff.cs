using System.Collections.Generic;
using System.Linq;
using LimeFlight.OpenAPI.Diff.BusinessObjects;
using LimeFlight.OpenAPI.Diff.Enums;
using LimeFlight.OpenAPI.Diff.Utils;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;

namespace LimeFlight.OpenAPI.Diff.Compare
{
	public class RequestBodyDiff : ReferenceDiffCache<OpenApiRequestBody, ChangedRequestBodyBO>
	{
		private static readonly RefPointer<OpenApiRequestBody> RefPointer =
			new RefPointer<OpenApiRequestBody>(RefTypeEnum.RequestBodies);

		private readonly OpenApiDiff _openApiDiff;

		public RequestBodyDiff(OpenApiDiff openApiDiff)
		{
			this._openApiDiff = openApiDiff;
		}

		private static IDictionary<string, IOpenApiExtension> GetExtensions(OpenApiRequestBody body)
		{
			return body.Extensions.ToDictionary(x => x.Key, x => x.Value);
		}

		public ChangedRequestBodyBO Diff(
			OpenApiRequestBody left, OpenApiRequestBody right, DiffContextBO context)
		{
			string leftRef = left.Reference?.ReferenceV3;
			string rightRef = right.Reference?.ReferenceV3;
			return this.CachedDiff(left, right, leftRef, rightRef, context);
		}

		protected override ChangedRequestBodyBO ComputeDiff(OpenApiRequestBody left,
			OpenApiRequestBody right,
			DiffContextBO context)
		{
			Dictionary<string, OpenApiMediaType> oldRequestContent = null;
			Dictionary<string, OpenApiMediaType> newRequestContent = null;
			OpenApiRequestBody oldRequestBody = null;
			OpenApiRequestBody newRequestBody = null;
			if (left != null)
			{
				oldRequestBody =
					RefPointer.ResolveRef(
						this._openApiDiff.OldSpecOpenApi.Components, left, left.Reference?.ReferenceV3);
				if (oldRequestBody.Content != null)
					oldRequestContent = (Dictionary<string, OpenApiMediaType>)oldRequestBody.Content;
			}

			if (right != null)
			{
				newRequestBody =
					RefPointer.ResolveRef(
						this._openApiDiff.NewSpecOpenApi.Components, right, right.Reference?.ReferenceV3);
				if (newRequestBody.Content != null)
					newRequestContent = (Dictionary<string, OpenApiMediaType>)newRequestBody.Content;
			}

			bool leftRequired =
				oldRequestBody != null && oldRequestBody.Required;
			bool rightRequired =
				newRequestBody != null && newRequestBody.Required;

			var changedRequestBody =
				new ChangedRequestBodyBO(oldRequestBody, newRequestBody, context)
				{
					ChangeRequired = leftRequired != rightRequired,
					Description = this._openApiDiff
						.MetadataDiff
						.Diff(
							oldRequestBody?.Description,
							newRequestBody?.Description,
							context),
					Content = this._openApiDiff
						.ContentDiff
						.Diff(oldRequestContent, newRequestContent, context),
					Extensions = this._openApiDiff
						.ExtensionsDiff
						.Diff(GetExtensions(left), GetExtensions(right), context)
				};

			return ChangedUtils.IsChanged(changedRequestBody);
		}
	}
}