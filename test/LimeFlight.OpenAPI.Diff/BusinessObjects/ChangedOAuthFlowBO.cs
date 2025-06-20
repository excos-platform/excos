using System.Collections.Generic;
using System.Linq;
using LimeFlight.OpenAPI.Diff.Enums;
using Microsoft.OpenApi.Models;

namespace LimeFlight.OpenAPI.Diff.BusinessObjects
{
	public class ChangedOAuthFlowBO : ComposedChangedBO
	{
		public ChangedOAuthFlowBO(OpenApiOAuthFlow oldOAuthFlow, OpenApiOAuthFlow newOAuthFlow)
		{
			this.OldOAuthFlow = oldOAuthFlow;
			this.NewOAuthFlow = newOAuthFlow;
		}

		public OpenApiOAuthFlow OldOAuthFlow { get; }
		public OpenApiOAuthFlow NewOAuthFlow { get; }

		public bool ChangedAuthorizationUrl { get; set; }
		public bool ChangedTokenUrl { get; set; }
		public bool ChangedRefreshUrl { get; set; }
		public ChangedExtensionsBO Extensions { get; set; }

		protected override ChangedElementTypeEnum GetElementType()
		{
			return ChangedElementTypeEnum.AuthFlow;
		}

		public override List<(string Identifier, ChangedBO Change)> GetChangedElements()
		{
			return new List<(string Identifier, ChangedBO Change)>
				{
					(null, this.Extensions)
				}
				.Where(x => x.Change != null).ToList();
		}

		public override DiffResultBO IsCoreChanged()
		{
			if (this.ChangedAuthorizationUrl || this.ChangedTokenUrl || this.ChangedRefreshUrl)
				return new DiffResultBO(DiffResultEnum.Incompatible);
			return new DiffResultBO(DiffResultEnum.NoChanges);
		}

		protected override List<ChangedInfoBO> GetCoreChanges()
		{
			var returnList = new List<ChangedInfoBO>();
			ChangedElementTypeEnum elementType = this.GetElementType();
			const TypeEnum changeType = TypeEnum.Changed;

			if (this.ChangedAuthorizationUrl)
				returnList.Add(new ChangedInfoBO(elementType, changeType, "AuthorizationUrl",
					this.OldOAuthFlow?.AuthorizationUrl.ToString(), this.NewOAuthFlow?.AuthorizationUrl.ToString()));

			if (this.ChangedTokenUrl)
				returnList.Add(new ChangedInfoBO(elementType, changeType, "TokenUrl", this.OldOAuthFlow?.TokenUrl.ToString(),
					this.NewOAuthFlow?.TokenUrl.ToString()));

			if (this.ChangedRefreshUrl)
				returnList.Add(new ChangedInfoBO(elementType, changeType, "RefreshUrl",
					this.OldOAuthFlow?.RefreshUrl.ToString(), this.NewOAuthFlow?.RefreshUrl.ToString()));

			return returnList;
		}
	}
}