using System.Collections.Generic;
using System.Linq;
using LimeFlight.OpenAPI.Diff.Enums;
using LimeFlight.OpenAPI.Diff.Extensions;
using Microsoft.OpenApi.Models;

namespace LimeFlight.OpenAPI.Diff.BusinessObjects
{
	public class ChangedContentBO : ComposedChangedBO
	{
		private readonly DiffContextBO _context;
		private readonly Dictionary<string, OpenApiMediaType> _newContent;

		private readonly Dictionary<string, OpenApiMediaType> _oldContent;

		public ChangedContentBO(Dictionary<string, OpenApiMediaType> oldContent,
			Dictionary<string, OpenApiMediaType> newContent, DiffContextBO context)
		{
			this._oldContent = oldContent;
			this._newContent = newContent;
			this._context = context;
			this.Increased = new Dictionary<string, OpenApiMediaType>();
			this.Missing = new Dictionary<string, OpenApiMediaType>();
			this.Changed = new Dictionary<string, ChangedMediaTypeBO>();
		}

		public Dictionary<string, OpenApiMediaType> Increased { get; set; }
		public Dictionary<string, OpenApiMediaType> Missing { get; set; }
		public Dictionary<string, ChangedMediaTypeBO> Changed { get; set; }

		protected override ChangedElementTypeEnum GetElementType()
		{
			return ChangedElementTypeEnum.Content;
		}

		public override List<(string Identifier, ChangedBO Change)> GetChangedElements()
		{
			return new List<(string Identifier, ChangedBO Change)>(
					this.Changed.Select(x => (x.Key, (ChangedBO)x.Value))
				)
				.Where(x => x.Change != null).ToList();
		}

		public override DiffResultBO IsCoreChanged()
		{
			if (this.Increased.IsNullOrEmpty() && this.Missing.IsNullOrEmpty()) return new DiffResultBO(DiffResultEnum.NoChanges);
			if (this._context.IsRequest && this.Missing.IsNullOrEmpty() || this._context.IsResponse && this.Increased.IsNullOrEmpty())
				return new DiffResultBO(DiffResultEnum.Compatible);
			return new DiffResultBO(DiffResultEnum.Incompatible);
		}

		protected override List<ChangedInfoBO> GetCoreChanges()
		{
			return this.GetCoreChangeInfosOfComposed(this.Increased.Keys.ToList(), this.Missing.Keys.ToList(), x => x);
		}
	}
}