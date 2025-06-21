using System.Collections.Generic;
using System.Linq;
using LimeFlight.OpenAPI.Diff.Enums;
using LimeFlight.OpenAPI.Diff.Extensions;
using Microsoft.OpenApi.Models;

namespace LimeFlight.OpenAPI.Diff.BusinessObjects
{
	public class ChangedAPIResponseBO : ComposedChangedBO
	{
		private readonly DiffContextBO _context;
		private readonly OpenApiResponses _newApiResponses;
		private readonly OpenApiResponses _oldApiResponses;

		public ChangedAPIResponseBO(OpenApiResponses oldApiResponses, OpenApiResponses newApiResponses,
			DiffContextBO context)
		{
			this._oldApiResponses = oldApiResponses;
			this._newApiResponses = newApiResponses;
			this._context = context;
			this.Increased = new Dictionary<string, OpenApiResponse>();
			this.Missing = new Dictionary<string, OpenApiResponse>();
			this.Changed = new Dictionary<string, ChangedResponseBO>();
		}

		public Dictionary<string, OpenApiResponse> Increased { get; set; }
		public Dictionary<string, OpenApiResponse> Missing { get; set; }
		public Dictionary<string, ChangedResponseBO> Changed { get; set; }
		public ChangedExtensionsBO Extensions { get; set; }

		protected override ChangedElementTypeEnum GetElementType()
		{
			return ChangedElementTypeEnum.Response;
		}

		public override List<(string Identifier, ChangedBO Change)> GetChangedElements()
		{
			return new List<(string Identifier, ChangedBO Change)>(
					this.Changed.Select(x => (x.Key, (ChangedBO)x.Value))
				)
				{
					(null, this.Extensions)
				}
				.Where(x => x.Change != null).ToList();
		}

		public override DiffResultBO IsCoreChanged()
		{
			if (this.Increased.IsNullOrEmpty() && this.Missing.IsNullOrEmpty()) return new DiffResultBO(DiffResultEnum.NoChanges);
			if (!this.Increased.IsNullOrEmpty() && this.Missing.IsNullOrEmpty())
				return new DiffResultBO(DiffResultEnum.Compatible);
			return new DiffResultBO(DiffResultEnum.Incompatible);
		}

		protected override List<ChangedInfoBO> GetCoreChanges()
		{
			return this.GetCoreChangeInfosOfComposed(this.Increased.Keys.ToList(), this.Missing.Keys.ToList(), x => x);
		}
	}
}