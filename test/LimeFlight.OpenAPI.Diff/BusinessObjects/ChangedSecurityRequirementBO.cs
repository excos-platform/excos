using System.Collections.Generic;
using System.Linq;
using LimeFlight.OpenAPI.Diff.Enums;
using Microsoft.OpenApi.Models;

namespace LimeFlight.OpenAPI.Diff.BusinessObjects
{
	public class ChangedSecurityRequirementBO : ComposedChangedBO
	{
		private readonly OpenApiSecurityRequirement _newSecurityRequirement;

		private readonly OpenApiSecurityRequirement _oldSecurityRequirement;

		public ChangedSecurityRequirementBO(OpenApiSecurityRequirement newSecurityRequirement,
			OpenApiSecurityRequirement oldSecurityRequirement)
		{
			this._newSecurityRequirement = newSecurityRequirement;
			this._oldSecurityRequirement = oldSecurityRequirement;
			this.Changed = new List<ChangedSecuritySchemeBO>();
		}

		public OpenApiSecurityRequirement Missing { get; set; }
		public OpenApiSecurityRequirement Increased { get; set; }
		public List<ChangedSecuritySchemeBO> Changed { get; set; }
		protected override ChangedElementTypeEnum GetElementType() => ChangedElementTypeEnum.SecurityRequirement;

		public override List<(string Identifier, ChangedBO Change)> GetChangedElements()
		{
			return new List<(string Identifier, ChangedBO Change)>(
					this.Changed.Select(x => (x.NewSecurityScheme.Name ?? x.OldSecurityScheme.Name, (ChangedBO)x))
				)
				.Where(x => x.Change != null).ToList();
		}

		public override DiffResultBO IsCoreChanged()
		{
			if (this.Increased == null && this.Missing == null) return new DiffResultBO(DiffResultEnum.NoChanges);
			if (this.Increased == null) return new DiffResultBO(DiffResultEnum.Compatible);
			return new DiffResultBO(DiffResultEnum.Incompatible);
		}

		protected override List<ChangedInfoBO> GetCoreChanges() =>
			this.GetCoreChangeInfosOfComposed(this.Increased.Keys.ToList(), this.Missing.Keys.ToList(), x => x.Name);
	}
}