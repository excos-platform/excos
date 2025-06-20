using System.Collections.Generic;
using System.Linq;
using LimeFlight.OpenAPI.Diff.Enums;
using Microsoft.OpenApi.Interfaces;

namespace LimeFlight.OpenAPI.Diff.BusinessObjects
{
	public class ChangedExtensionsBO : ComposedChangedBO
	{
		private readonly DiffContextBO _context;
		private readonly Dictionary<string, IOpenApiExtension> _newExtensions;

		private readonly Dictionary<string, IOpenApiExtension> _oldExtensions;

		public ChangedExtensionsBO(Dictionary<string, IOpenApiExtension> oldExtensions,
			Dictionary<string, IOpenApiExtension> newExtensions, DiffContextBO context)
		{
			this._oldExtensions = oldExtensions;
			this._newExtensions = newExtensions;
			this._context = context;
			this.Increased = new Dictionary<string, ChangedBO>();
			this.Missing = new Dictionary<string, ChangedBO>();
			this.Changed = new Dictionary<string, ChangedBO>();
		}

		public Dictionary<string, ChangedBO> Increased { get; set; }
		public Dictionary<string, ChangedBO> Missing { get; set; }
		public Dictionary<string, ChangedBO> Changed { get; set; }

		protected override ChangedElementTypeEnum GetElementType()
		{
			return ChangedElementTypeEnum.Extension;
		}

		public override List<(string Identifier, ChangedBO Change)> GetChangedElements()
		{
			return new List<(string Identifier, ChangedBO Change)>()
				.Concat(this.Increased.Select(x => (x.Key, x.Value)))
				.Concat(this.Missing.Select(x => (x.Key, x.Value)))
				.Concat(this.Changed.Select(x => (x.Key, x.Value)))
				.Where(x => x.Item2 != null).ToList();
		}

		public override DiffResultBO IsCoreChanged()
		{
			return new DiffResultBO(DiffResultEnum.NoChanges);
		}

		protected override List<ChangedInfoBO> GetCoreChanges()
		{
			return new List<ChangedInfoBO>();
		}
	}
}