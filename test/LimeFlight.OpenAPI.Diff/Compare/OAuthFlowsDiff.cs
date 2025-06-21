using LimeFlight.OpenAPI.Diff.BusinessObjects;
using LimeFlight.OpenAPI.Diff.Utils;
using Microsoft.OpenApi.Models;

namespace LimeFlight.OpenAPI.Diff.Compare
{
	public class OAuthFlowsDiff
	{
		private readonly OpenApiDiff _openApiDiff;

		public OAuthFlowsDiff(OpenApiDiff openApiDiff)
		{
			this._openApiDiff = openApiDiff;
		}

		public ChangedOAuthFlowsBO Diff(OpenApiOAuthFlows left, OpenApiOAuthFlows right)
		{
			var changedOAuthFlows = new ChangedOAuthFlowsBO(left, right);
			if (left != null && right != null)
			{
				changedOAuthFlows.ImplicitOAuthFlow = this._openApiDiff
					.OAuthFlowDiff
					.Diff(left.Implicit, right.Implicit);
				changedOAuthFlows.PasswordOAuthFlow = this._openApiDiff
					.OAuthFlowDiff
					.Diff(left.Password, right.Password);
				changedOAuthFlows.ClientCredentialOAuthFlow = this._openApiDiff
					.OAuthFlowDiff
					.Diff(left.ClientCredentials, right.ClientCredentials);
				changedOAuthFlows.AuthorizationCodeOAuthFlow = this._openApiDiff
					.OAuthFlowDiff
					.Diff(left.AuthorizationCode, right.AuthorizationCode);
			}

			changedOAuthFlows.Extensions = this._openApiDiff
				.ExtensionsDiff
				.Diff(left?.Extensions, right?.Extensions);

			return ChangedUtils.IsChanged(changedOAuthFlows);
		}
	}
}