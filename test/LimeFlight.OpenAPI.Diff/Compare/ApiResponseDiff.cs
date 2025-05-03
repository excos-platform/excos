using System.Collections.Generic;
using LimeFlight.OpenAPI.Diff.BusinessObjects;
using LimeFlight.OpenAPI.Diff.Utils;
using Microsoft.OpenApi.Models;

namespace LimeFlight.OpenAPI.Diff.Compare
{
	public class ApiResponseDiff
	{
		private readonly OpenApiDiff _openApiDiff;

		public ApiResponseDiff(OpenApiDiff openApiDiff)
		{
			this._openApiDiff = openApiDiff;
		}

		public ChangedAPIResponseBO Diff(OpenApiResponses left, OpenApiResponses right, DiffContextBO context)
		{
			var responseMapKeyDiff = MapKeyDiff<string, OpenApiResponse>.Diff(left, right);
			List<string> sharedResponseCodes = responseMapKeyDiff.SharedKey;
			var responses = new Dictionary<string, ChangedResponseBO>();
			foreach (string responseCode in sharedResponseCodes)
			{
				ChangedResponseBO diff = this._openApiDiff
					.ResponseDiff
					.Diff(left[responseCode], right[responseCode], context);

				if (diff != null)
					responses.Add(responseCode, diff);
			}

			var changedApiResponse =
				new ChangedAPIResponseBO(left, right, context)
				{
					Increased = responseMapKeyDiff.Increased,
					Missing = responseMapKeyDiff.Missing,
					Changed = responses,
					Extensions = this._openApiDiff
						.ExtensionsDiff
						.Diff(left.Extensions, right.Extensions, context)
				};

			return ChangedUtils.IsChanged(changedApiResponse);
		}
	}
}