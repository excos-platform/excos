using System.Collections.Generic;
using System.Linq;
using LimeFlight.OpenAPI.Diff.Enums;
using Microsoft.OpenApi.Models;

namespace LimeFlight.OpenAPI.Diff.BusinessObjects
{
	public class ChangedParameterBO : ComposedChangedBO
	{
		private readonly DiffContextBO _context;

		public ChangedParameterBO(string name, ParameterLocation? @in, OpenApiParameter oldParameter,
			OpenApiParameter newParameter, DiffContextBO context)
		{
			this._context = context;
			this.Name = name;
			this.In = @in;
			this.OldParameter = oldParameter;
			this.NewParameter = newParameter;
		}

		public ParameterLocation? In { get; set; }
		public string Name { get; set; }
		public OpenApiParameter OldParameter { get; }
		public OpenApiParameter NewParameter { get; }
		public bool IsChangeRequired { get; set; }
		public bool IsDeprecated { get; set; }
		public bool ChangeStyle { get; set; }
		public bool ChangeExplode { get; set; }
		public bool ChangeAllowEmptyValue { get; set; }
		public ChangedMetadataBO Description { get; set; }
		public ChangedSchemaBO Schema { get; set; }
		public ChangedContentBO Content { get; set; }
		public ChangedExtensionsBO Extensions { get; set; }

		protected override ChangedElementTypeEnum GetElementType()
		{
			return ChangedElementTypeEnum.Parameter;
		}

		public override List<(string Identifier, ChangedBO Change)> GetChangedElements()
		{
			return new List<(string Identifier, ChangedBO Change)>
				{
					(null, this.Description),
					(null, this.Schema),
					(null, this.Content),
					(null, this.Extensions)
				}
				.Where(x => x.Change != null).ToList();
		}

		public override DiffResultBO IsCoreChanged()
		{
			if (!this.IsChangeRequired
				&& !this.IsDeprecated
				&& !this.ChangeAllowEmptyValue
				&& !this.ChangeStyle
				&& !this.ChangeExplode)
				return new DiffResultBO(DiffResultEnum.NoChanges);
			if ((!this.IsChangeRequired || this.OldParameter.Required)
				&& (!this.ChangeAllowEmptyValue || this.NewParameter.AllowEmptyValue)
				&& !this.ChangeStyle
				&& !this.ChangeExplode)
				return new DiffResultBO(DiffResultEnum.Compatible);
			return new DiffResultBO(DiffResultEnum.Incompatible);
		}

		protected override List<ChangedInfoBO> GetCoreChanges()
		{
			var returnList = new List<ChangedInfoBO>();
			var elementType = this.GetElementType();
			const TypeEnum changeType = TypeEnum.Changed;

			if (this.IsChangeRequired)
				returnList.Add(new ChangedInfoBO(elementType, changeType, "Required", this.OldParameter?.Required.ToString(),
					this.NewParameter?.Required.ToString()));

			if (this.IsDeprecated)
				returnList.Add(new ChangedInfoBO(elementType, changeType, "Deprecation",
					this.OldParameter?.Deprecated.ToString(), this.NewParameter?.Deprecated.ToString()));

			if (this.ChangeStyle)
				returnList.Add(new ChangedInfoBO(elementType, changeType, "Style", this.OldParameter?.Style.ToString(),
					this.NewParameter?.Style.ToString()));

			if (this.ChangeExplode)
				returnList.Add(new ChangedInfoBO(elementType, changeType, "Explode", this.OldParameter?.Explode.ToString(),
					this.NewParameter?.Explode.ToString()));

			if (this.ChangeAllowEmptyValue)
				returnList.Add(new ChangedInfoBO(elementType, changeType, "AllowEmptyValue",
					this.OldParameter?.AllowEmptyValue.ToString(), this.NewParameter?.AllowEmptyValue.ToString()));

			return returnList;
		}
	}
}