using System.Collections.Generic;
using System.Linq;
using LimeFlight.OpenAPI.Diff.Enums;
using LimeFlight.OpenAPI.Diff.Extensions;
using Microsoft.OpenApi.Models;

namespace LimeFlight.OpenAPI.Diff.BusinessObjects
{
	public class ChangedPathsBO : ComposedChangedBO
	{
		private readonly Dictionary<string, OpenApiPathItem> _newPathMap;

		private readonly Dictionary<string, OpenApiPathItem> _oldPathMap;

		public ChangedPathsBO(Dictionary<string, OpenApiPathItem> oldPathMap,
			Dictionary<string, OpenApiPathItem> newPathMap)
		{
			this._oldPathMap = oldPathMap;
			this._newPathMap = newPathMap;
			this.Increased = new Dictionary<string, OpenApiPathItem>();
			this.Missing = new Dictionary<string, OpenApiPathItem>();
			this.Changed = new Dictionary<string, ChangedPathBO>();
		}

		public Dictionary<string, OpenApiPathItem> Increased { get; set; }
		public Dictionary<string, OpenApiPathItem> Missing { get; set; }
		public Dictionary<string, ChangedPathBO> Changed { get; set; }
		protected override ChangedElementTypeEnum GetElementType() => ChangedElementTypeEnum.Path;

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

		protected override List<ChangedInfoBO> GetCoreChanges() =>
			this.GetCoreChangeInfosOfComposed(this.Increased.ToList(), this.Missing.ToList(), x => x.Key);
	}
}