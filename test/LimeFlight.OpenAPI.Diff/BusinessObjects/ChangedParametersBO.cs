using System.Collections.Generic;
using System.Linq;
using LimeFlight.OpenAPI.Diff.Enums;
using LimeFlight.OpenAPI.Diff.Extensions;
using Microsoft.OpenApi.Models;

namespace LimeFlight.OpenAPI.Diff.BusinessObjects
{
	public class ChangedParametersBO : ComposedChangedBO
	{
		private readonly DiffContextBO _context;
		private readonly List<OpenApiParameter> _newParameterList;

		private readonly List<OpenApiParameter> _oldParameterList;

		public ChangedParametersBO(List<OpenApiParameter> oldParameterList, List<OpenApiParameter> newParameterList,
			DiffContextBO context)
		{
			this._oldParameterList = oldParameterList;
			this._newParameterList = newParameterList;
			this._context = context;
			this.Increased = new List<OpenApiParameter>();
			this.Missing = new List<OpenApiParameter>();
			this.Changed = new List<ChangedParameterBO>();
		}

		public List<OpenApiParameter> Increased { get; set; }
		public List<OpenApiParameter> Missing { get; set; }
		public List<ChangedParameterBO> Changed { get; set; }
		protected override ChangedElementTypeEnum GetElementType() => ChangedElementTypeEnum.Parameter;

		public override List<(string Identifier, ChangedBO Change)> GetChangedElements()
		{
			return new List<(string Identifier, ChangedBO Change)>(
					this.Changed.Select(x => (x.Name, (ChangedBO)x))
				)
				.Where(x => x.Change != null).ToList();
		}

		public override DiffResultBO IsCoreChanged()
		{
			if (this.Increased.IsNullOrEmpty() && this.Missing.IsNullOrEmpty()) return new DiffResultBO(DiffResultEnum.NoChanges);

			if (!this.Increased.Any(x => x.Required) && this.Missing.IsNullOrEmpty())
				return new DiffResultBO(DiffResultEnum.Compatible);

			return new DiffResultBO(DiffResultEnum.Incompatible);
		}

		protected override List<ChangedInfoBO> GetCoreChanges() =>
			this.GetCoreChangeInfosOfComposed(this.Increased, this.Missing, x => x.Name);
	}
}