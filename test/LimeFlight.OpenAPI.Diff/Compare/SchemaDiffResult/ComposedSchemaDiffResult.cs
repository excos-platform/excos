using System;
using System.Collections.Generic;
using System.Linq;
using LimeFlight.OpenAPI.Diff.BusinessObjects;
using LimeFlight.OpenAPI.Diff.Enums;
using LimeFlight.OpenAPI.Diff.Extensions;
using LimeFlight.OpenAPI.Diff.Utils;
using Microsoft.OpenApi.Models;

namespace LimeFlight.OpenAPI.Diff.Compare.SchemaDiffResult
{
	public class ComposedSchemaDiffResult : SchemaDiffResult
	{
		private static readonly RefPointer<OpenApiSchema> refPointer =
			new RefPointer<OpenApiSchema>(RefTypeEnum.Schemas);

		public ComposedSchemaDiffResult(OpenApiDiff openApiDiff) : base(openApiDiff)
		{
		}

		private static Dictionary<string, OpenApiSchema> GetSchema(OpenApiComponents components,
			Dictionary<string, string> mapping)
		{
			var result = new Dictionary<string, OpenApiSchema>();
			foreach (KeyValuePair<string, string> map in mapping)
				result.Add(map.Key, refPointer.ResolveRef(components, new OpenApiSchema(), map.Value));
			return result;
		}

		private static Dictionary<string, string> GetMapping(OpenApiSchema composedSchema)
		{
			if (composedSchema.GetSchemaType() != SchemaTypeEnum.ComposedSchema)
				return null;

			var reverseMapping = new Dictionary<string, string>();
			foreach (OpenApiSchema schema in composedSchema.OneOf)
			{
				string schemaRef = schema.Reference?.ReferenceV3;
				if (schemaRef == null) throw new ArgumentNullException("invalid oneOf schema");
				string schemaName = refPointer.GetRefName(schemaRef);
				if (schemaName == null) throw new ArgumentNullException("invalid schema: " + schemaRef);
				reverseMapping.Add(schemaRef, schemaName);
			}

			if (composedSchema.Discriminator != null && !composedSchema.Discriminator.Mapping.IsNullOrEmpty())
				foreach ((string key, string value) in composedSchema.Discriminator.Mapping)
					if (!reverseMapping.TryAdd(value, key))
						reverseMapping[value] = key;

			return reverseMapping.ToDictionary(x => x.Value, x => x.Key);
		}

		public override ChangedSchemaBO Diff<T>(OpenApiComponents leftComponents,
			OpenApiComponents rightComponents, T left,
			T right, DiffContextBO context)
		{
			if (left.GetSchemaType() == SchemaTypeEnum.ComposedSchema)
			{
				if (!left.OneOf.IsNullOrEmpty() || !right.OneOf.IsNullOrEmpty())
				{
					OpenApiDiscriminator leftDis = left.Discriminator;
					OpenApiDiscriminator rightDis = right.Discriminator;

					if ((leftDis == null && rightDis != null)
						|| (leftDis != null && rightDis == null)
						|| (leftDis != null
							&& ((leftDis.PropertyName == null && rightDis.PropertyName != null)
								|| (leftDis.PropertyName != null && rightDis.PropertyName == null)
								|| (leftDis.PropertyName != null
									&& rightDis.PropertyName != null
									&& !leftDis.PropertyName.Equals(rightDis.PropertyName)))))
					{
						this.ChangedSchema.OldSchema = left;
						this.ChangedSchema.NewSchema = right;
						this.ChangedSchema.DiscriminatorPropertyChanged = true;
						this.ChangedSchema.Context = context;
						return this.ChangedSchema;
					}

					Dictionary<string, string> leftMapping = GetMapping(left);
					Dictionary<string, string> rightMapping = GetMapping(right);

					var mappingDiff = MapKeyDiff<string, OpenApiSchema>.Diff(GetSchema(leftComponents, leftMapping),
						GetSchema(rightComponents, rightMapping));
					var changedMapping = new Dictionary<string, ChangedSchemaBO>();
					foreach (string refId in mappingDiff.SharedKey)
					{
						OpenApiReference leftReference = leftComponents.Schemas.Values
							.First(x => x.Reference.ReferenceV3 == leftMapping[refId]).Reference;
						OpenApiReference rightReference = rightComponents.Schemas.Values
							.First(x => x.Reference.ReferenceV3 == rightMapping[refId]).Reference;

						var leftSchema = new OpenApiSchema { Reference = leftReference };
						var rightSchema = new OpenApiSchema { Reference = rightReference };
						ChangedSchemaBO changedSchema = this.OpenApiDiff.SchemaDiff
							.Diff(leftSchema, rightSchema, context.CopyWithRequired(true));
						if (changedSchema != null)
							changedMapping.Add(refId, changedSchema);
					}

					this.ChangedSchema.OneOfSchema = new ChangedOneOfSchemaBO(leftMapping, rightMapping, context)
					{
						Increased = mappingDiff.Increased,
						Missing = mappingDiff.Missing,
						Changed = changedMapping
					};
				}

				return base.Diff(leftComponents, rightComponents, left, right, context);
			}

			return this.OpenApiDiff.SchemaDiff.GetTypeChangedSchema(left, right, context);
		}
	}
}