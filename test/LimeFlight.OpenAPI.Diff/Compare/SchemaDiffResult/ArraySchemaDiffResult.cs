﻿using System.Collections.Generic;
using LimeFlight.OpenAPI.Diff.BusinessObjects;
using LimeFlight.OpenAPI.Diff.Enums;
using LimeFlight.OpenAPI.Diff.Extensions;
using Microsoft.OpenApi.Models;

namespace LimeFlight.OpenAPI.Diff.Compare.SchemaDiffResult
{
	public class ArraySchemaDiffResult : SchemaDiffResult
	{
		public ArraySchemaDiffResult(OpenApiDiff openApiDiff) : base("array", openApiDiff)
		{
		}

		public override ChangedSchemaBO Diff<T>(OpenApiComponents leftComponents,
			OpenApiComponents rightComponents, T left,
			T right, DiffContextBO context)
		{
			if (left.GetSchemaType() != SchemaTypeEnum.ArraySchema
				|| right.GetSchemaType() != SchemaTypeEnum.ArraySchema)
				return null;

			base.Diff(leftComponents, rightComponents, left, right, context);

			ChangedSchemaBO diff = this.OpenApiDiff
				.SchemaDiff
				.Diff(
					left.Items,
					right.Items,
					context.CopyWithRequired(true));
			if (diff != null)
				this.ChangedSchema.Items = diff;

			return this.IsApplicable(context);
		}
	}
}