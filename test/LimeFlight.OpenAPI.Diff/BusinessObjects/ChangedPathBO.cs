using System.Collections.Generic;
using System.Linq;
using LimeFlight.OpenAPI.Diff.Enums;
using LimeFlight.OpenAPI.Diff.Extensions;
using Microsoft.OpenApi.Models;

namespace LimeFlight.OpenAPI.Diff.BusinessObjects
{
	public class ChangedPathBO : ComposedChangedBO
	{
		private readonly DiffContextBO _context;
		private readonly OpenApiPathItem _newPath;
		private readonly OpenApiPathItem _oldPath;

		private readonly string _pathUrl;

		public ChangedPathBO(string pathUrl, OpenApiPathItem oldPath, OpenApiPathItem newPath, DiffContextBO context)
		{
			this._pathUrl = pathUrl;
			this._oldPath = oldPath;
			this._newPath = newPath;
			this._context = context;
			this.Increased = new Dictionary<OperationType, OpenApiOperation>();
			this.Missing = new Dictionary<OperationType, OpenApiOperation>();
			this.Changed = new List<ChangedOperationBO>();
		}

		public Dictionary<OperationType, OpenApiOperation> Increased { get; set; }
		public Dictionary<OperationType, OpenApiOperation> Missing { get; set; }
		public List<ChangedOperationBO> Changed { get; set; }
		public ChangedExtensionsBO Extensions { get; set; }
		protected override ChangedElementTypeEnum GetElementType() => ChangedElementTypeEnum.Path;

		public override List<(string Identifier, ChangedBO Change)> GetChangedElements()
		{
			return new List<(string Identifier, ChangedBO Change)>(
					this.Changed.Select(x => (x.PathUrl, (ChangedBO)x))
				)
				{
					(null, this.Extensions)
				}
				.Where(x => x.Change != null).ToList();
		}

		public override DiffResultBO IsCoreChanged()
		{
			if (this.Increased.IsNullOrEmpty() && this.Missing.IsNullOrEmpty()) return new DiffResultBO(DiffResultEnum.NoChanges);
			if (this.Missing.IsNullOrEmpty()) return new DiffResultBO(DiffResultEnum.Compatible);
			return new DiffResultBO(DiffResultEnum.Incompatible);
		}

		protected override List<ChangedInfoBO> GetCoreChanges() =>
			this.GetCoreChangeInfosOfComposed(this.Increased.Keys.ToList(), this.Missing.Keys.ToList(), x => x.ToString());
	}
}