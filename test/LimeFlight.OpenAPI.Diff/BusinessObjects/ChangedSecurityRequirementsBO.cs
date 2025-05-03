using System.Collections.Generic;
using System.Linq;
using LimeFlight.OpenAPI.Diff.Enums;
using LimeFlight.OpenAPI.Diff.Extensions;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;

namespace LimeFlight.OpenAPI.Diff.BusinessObjects
{
	public class ChangedSecurityRequirementsBO : ComposedChangedBO
	{
		private readonly IList<OpenApiSecurityRequirement> _newSecurityRequirements;

		private readonly IList<OpenApiSecurityRequirement> _oldSecurityRequirements;

		public ChangedSecurityRequirementsBO(IList<OpenApiSecurityRequirement> oldSecurityRequirements,
			IList<OpenApiSecurityRequirement> newSecurityRequirements)
		{
			this._oldSecurityRequirements = oldSecurityRequirements;
			this._newSecurityRequirements = newSecurityRequirements;
			this.Missing = new List<OpenApiSecurityRequirement>();
			this.Increased = new List<OpenApiSecurityRequirement>();
			this.Changed = new List<ChangedSecurityRequirementBO>();
		}

		public List<OpenApiSecurityRequirement> Missing { get; set; }
		public List<OpenApiSecurityRequirement> Increased { get; set; }
		public List<ChangedSecurityRequirementBO> Changed { get; set; }
		protected override ChangedElementTypeEnum GetElementType() => ChangedElementTypeEnum.SecurityRequirement;

		public override List<(string Identifier, ChangedBO Change)> GetChangedElements()
		{
			return new List<(string Identifier, ChangedBO Change)>(
					this.Changed.Select(x => (this.GetElementType().GetDisplayName(), (ChangedBO)x))
				)
				.Where(x => x.Change != null).ToList();
		}

		public override DiffResultBO IsCoreChanged()
		{
			if (this.Missing.IsNullOrEmpty() && this.Increased.IsNullOrEmpty()) return new DiffResultBO(DiffResultEnum.NoChanges);
			if (this.Missing.IsNullOrEmpty()) return new DiffResultBO(DiffResultEnum.Compatible);
			return new DiffResultBO(DiffResultEnum.Incompatible);
		}

		protected override List<ChangedInfoBO> GetCoreChanges() =>
			this.GetCoreChangeInfosOfComposed(this.Increased.SelectMany(x => x.Keys).ToList(),
				this.Missing.SelectMany(x => x.Keys).ToList(), x => x.Name);
	}
}