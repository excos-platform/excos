using System.Collections.Generic;
using System.Linq;
using LimeFlight.OpenAPI.Diff.Enums;
using LimeFlight.OpenAPI.Diff.Extensions;
using Microsoft.OpenApi.Models;

namespace LimeFlight.OpenAPI.Diff.BusinessObjects
{
	public class ChangedHeadersBO : ComposedChangedBO
	{
		private readonly DiffContextBO _context;
		private readonly IDictionary<string, OpenApiHeader> _newHeaders;

		private readonly IDictionary<string, OpenApiHeader> _oldHeaders;

		public ChangedHeadersBO(IDictionary<string, OpenApiHeader> oldHeaders,
			IDictionary<string, OpenApiHeader> newHeaders, DiffContextBO context)
		{
			this._oldHeaders = oldHeaders;
			this._newHeaders = newHeaders;
			this._context = context;
		}

		public Dictionary<string, OpenApiHeader> Increased { get; set; }
		public Dictionary<string, OpenApiHeader> Missing { get; set; }
		public Dictionary<string, ChangedHeaderBO> Changed { get; set; }

		protected override ChangedElementTypeEnum GetElementType()
		{
			return ChangedElementTypeEnum.Header;
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
			if (this.Missing.IsNullOrEmpty()) return new DiffResultBO(DiffResultEnum.Compatible);
			return new DiffResultBO(DiffResultEnum.Incompatible);
		}

		protected override List<ChangedInfoBO> GetCoreChanges()
		{
			return this.GetCoreChangeInfosOfComposed(this.Increased.Keys.ToList(), this.Missing.Keys.ToList(), x => x);
		}
	}
}