using System.Collections.Generic;
using System.Linq;
using LimeFlight.OpenAPI.Diff.Enums;
using Microsoft.OpenApi.Models;

namespace LimeFlight.OpenAPI.Diff.BusinessObjects
{
	public class ChangedRequestBodyBO : ComposedChangedBO
	{
		private readonly DiffContextBO _context;
		private readonly OpenApiRequestBody _newRequestBody;

		private readonly OpenApiRequestBody _oldRequestBody;

		public ChangedRequestBodyBO(OpenApiRequestBody oldRequestBody, OpenApiRequestBody newRequestBody,
			DiffContextBO context)
		{
			this._oldRequestBody = oldRequestBody;
			this._newRequestBody = newRequestBody;
			this._context = context;
		}

		public bool ChangeRequired { get; set; }
		public ChangedMetadataBO Description { get; set; }
		public ChangedContentBO Content { get; set; }
		public ChangedExtensionsBO Extensions { get; set; }
		protected override ChangedElementTypeEnum GetElementType() => ChangedElementTypeEnum.RequestBody;

		public override List<(string Identifier, ChangedBO Change)> GetChangedElements()
		{
			return new List<(string Identifier, ChangedBO Change)>
				{
					("Description", this.Description),
					("Content", this.Content),
					(null, this.Extensions)
				}
				.Where(x => x.Change != null).ToList();
		}

		public override DiffResultBO IsCoreChanged()
		{
			if (!this.ChangeRequired) return new DiffResultBO(DiffResultEnum.NoChanges);
			return new DiffResultBO(DiffResultEnum.Incompatible);
		}

		protected override List<ChangedInfoBO> GetCoreChanges()
		{
			var returnList = new List<ChangedInfoBO>();
			ChangedElementTypeEnum elementType = this.GetElementType();
			const TypeEnum changeType = TypeEnum.Changed;

			if (this._oldRequestBody?.Required != this._newRequestBody?.Required)
				returnList.Add(new ChangedInfoBO(elementType, changeType, "Required",
					this._oldRequestBody?.Required.ToString(), this._newRequestBody?.Required.ToString()));

			return returnList;
		}
	}
}