using System.Collections.Generic;
using System.Linq;
using LimeFlight.OpenAPI.Diff.Enums;
using Microsoft.OpenApi.Models;

namespace LimeFlight.OpenAPI.Diff.BusinessObjects
{
	public class ChangedHeaderBO : ComposedChangedBO
	{
		private readonly DiffContextBO _context;

		public ChangedHeaderBO(OpenApiHeader oldHeader, OpenApiHeader newHeader, DiffContextBO context)
		{
			this.OldHeader = oldHeader;
			this.NewHeader = newHeader;
			this._context = context;
		}

		public OpenApiHeader OldHeader { get; }
		public OpenApiHeader NewHeader { get; }

		public bool Required { get; set; }
		public bool Deprecated { get; set; }
		public bool Style { get; set; }
		public bool Explode { get; set; }
		public ChangedMetadataBO Description { get; set; }
		public ChangedSchemaBO Schema { get; set; }
		public ChangedContentBO Content { get; set; }
		public ChangedExtensionsBO Extensions { get; set; }

		protected override ChangedElementTypeEnum GetElementType()
		{
			return ChangedElementTypeEnum.Header;
		}

		public override List<(string Identifier, ChangedBO Change)> GetChangedElements()
		{
			return new List<(string Identifier, ChangedBO Change)>
				{
					("Description", this.Description),
					("Schema", this.Schema),
					("Content", this.Content),
					(null, this.Extensions)
				}
				.Where(x => x.Change != null).ToList();
		}

		public override DiffResultBO IsCoreChanged()
		{
			if (!this.Required && !this.Deprecated && !this.Style && !this.Explode) return new DiffResultBO(DiffResultEnum.NoChanges);
			if (!this.Required && !this.Style && !this.Explode) return new DiffResultBO(DiffResultEnum.Compatible);
			return new DiffResultBO(DiffResultEnum.Incompatible);
		}

		protected override List<ChangedInfoBO> GetCoreChanges()
		{
			var returnList = new List<ChangedInfoBO>();
			ChangedElementTypeEnum elementType = this.GetElementType();
			const TypeEnum changeType = TypeEnum.Changed;

			if (this.Required)
				returnList.Add(new ChangedInfoBO(elementType, changeType, "Required", this.OldHeader?.Required.ToString(),
					this.NewHeader?.Required.ToString()));

			if (this.Deprecated)
				returnList.Add(new ChangedInfoBO(elementType, changeType, "Deprecation",
					this.OldHeader?.Deprecated.ToString(), this.NewHeader?.Deprecated.ToString()));

			if (this.Style)
				returnList.Add(new ChangedInfoBO(elementType, changeType, "Style", this.OldHeader?.Style.ToString(),
					this.NewHeader?.Style.ToString()));

			if (this.Explode)
				returnList.Add(new ChangedInfoBO(elementType, changeType, "Explode", this.OldHeader?.Explode.ToString(),
					this.NewHeader?.Explode.ToString()));

			return returnList;
		}
	}
}