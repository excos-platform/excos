using System.Collections.Generic;
using System.Linq;
using LimeFlight.OpenAPI.Diff.Enums;
using LimeFlight.OpenAPI.Diff.Extensions;
using Microsoft.OpenApi.Models;

namespace LimeFlight.OpenAPI.Diff.BusinessObjects
{
	public class ChangedSchemaBO : ComposedChangedBO
	{
		public ChangedSchemaBO()
		{
			this.IncreasedProperties = new Dictionary<string, OpenApiSchema>();
			this.MissingProperties = new Dictionary<string, OpenApiSchema>();
			this.ChangedProperties = new Dictionary<string, ChangedSchemaBO>();
		}

		public DiffContextBO Context { get; set; }
		public OpenApiSchema OldSchema { get; set; }
		public OpenApiSchema NewSchema { get; set; }
		public string Type { get; set; }
		public Dictionary<string, ChangedSchemaBO> ChangedProperties { get; set; }
		public Dictionary<string, OpenApiSchema> IncreasedProperties { get; set; }
		public Dictionary<string, OpenApiSchema> MissingProperties { get; set; }
		public bool IsChangeDeprecated { get; set; }
		public ChangedMetadataBO Description { get; set; }
		public bool IsChangeTitle { get; set; }
		public ChangedRequiredBO Required { get; set; }
		public bool IsChangeDefault { get; set; }
		public ChangedEnumBO Enumeration { get; set; }
		public bool IsChangeFormat { get; set; }
		public ChangedReadOnlyBO ReadOnly { get; set; }
		public ChangedWriteOnlyBO WriteOnly { get; set; }
		public bool IsChangedType { get; set; }
		public ChangedMinLengthBO MinLength { get; set; }
		public ChangedMaxLengthBO MaxLength { get; set; }
		public bool DiscriminatorPropertyChanged { get; set; }
		public ChangedSchemaBO Items { get; set; }
		public ChangedOneOfSchemaBO OneOfSchema { get; set; }
		public ChangedSchemaBO AddProp { get; set; }
		public ChangedExtensionsBO Extensions { get; set; }
		protected override ChangedElementTypeEnum GetElementType() => ChangedElementTypeEnum.Schema;

		public override List<(string Identifier, ChangedBO Change)> GetChangedElements()
		{
			return new List<(string Identifier, ChangedBO Change)>(
					this.ChangedProperties.Select(x => (x.Key, (ChangedBO)x.Value))
				)
				{
					("Description", this.Description),
					("ReadOnly", this.ReadOnly),
					("WriteOnly", this.WriteOnly),
					("Items", this.Items),
					("OneOfSchema", this.OneOfSchema),
					("AddProp", this.AddProp),
					("Enumeration", this.Enumeration),
					("Required", this.Required),
					("MinLength", this.MinLength),
					("MaxLength", this.MaxLength),
					(null, this.Extensions)
				}
				.Where(x => x.Change != null).ToList();
		}

		public override DiffResultBO IsCoreChanged()
		{
			if (
				!this.IsChangedType
				&& (this.OldSchema == null && this.NewSchema == null || this.OldSchema != null && this.NewSchema != null)
				&& !this.IsChangeFormat
				&& this.IncreasedProperties.Count == 0
				&& this.MissingProperties.Count == 0
				&& this.ChangedProperties.Values.Count == 0
				&& !this.IsChangeDeprecated
				&& !this.DiscriminatorPropertyChanged
			)
				return new DiffResultBO(DiffResultEnum.NoChanges);

			bool compatibleForRequest = this.OldSchema != null || this.NewSchema == null;
			bool compatibleForResponse =
				this.MissingProperties.IsNullOrEmpty() && (this.OldSchema == null || this.NewSchema != null);

			if ((this.Context.IsRequest && compatibleForRequest
				 || this.Context.IsResponse && compatibleForResponse)
				&& !this.IsChangedType
				&& !this.DiscriminatorPropertyChanged)
				return new DiffResultBO(DiffResultEnum.Compatible);
			return new DiffResultBO(DiffResultEnum.Incompatible);
		}

		protected override List<ChangedInfoBO> GetCoreChanges()
		{
			List<ChangedInfoBO> returnList = this.GetCoreChangeInfosOfComposed(this.IncreasedProperties.Keys.ToList(),
				this.MissingProperties.Keys.ToList(), x => x);
			ChangedElementTypeEnum elementType = this.GetElementType();
			const TypeEnum changeType = TypeEnum.Changed;

			if (this.IsChangedType)
				returnList.Add(new ChangedInfoBO(elementType, changeType, "Type", this.OldSchema?.Type, this.NewSchema?.Type));

			if (this.IsChangeDefault)
				returnList.Add(new ChangedInfoBO(elementType, changeType, "Default", this.OldSchema?.Default.ToString(),
					this.NewSchema?.Default.ToString()));

			if (this.IsChangeDeprecated)
				returnList.Add(new ChangedInfoBO(elementType, changeType, "Deprecation",
					this.OldSchema?.Deprecated.ToString(), this.NewSchema?.Deprecated.ToString()));

			if (this.IsChangeFormat)
				returnList.Add(new ChangedInfoBO(elementType, changeType, "Format", this.OldSchema?.Format,
					this.NewSchema?.Format));

			if (this.IsChangeTitle)
				returnList.Add(new ChangedInfoBO(elementType, changeType, "Title", this.OldSchema?.Title, this.NewSchema?.Title));

			if (this.DiscriminatorPropertyChanged)
				returnList.Add(new ChangedInfoBO(elementType, changeType, "Discriminator Property",
					this.OldSchema?.Discriminator?.PropertyName, this.NewSchema?.Discriminator?.PropertyName));

			return returnList;
		}
	}
}