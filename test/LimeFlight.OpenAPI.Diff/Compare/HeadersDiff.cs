using System.Collections.Generic;
using LimeFlight.OpenAPI.Diff.BusinessObjects;
using LimeFlight.OpenAPI.Diff.Utils;
using Microsoft.OpenApi.Models;

namespace LimeFlight.OpenAPI.Diff.Compare
{
	public class HeadersDiff
	{
		private readonly OpenApiDiff _openApiDiff;

		public HeadersDiff(OpenApiDiff openApiDiff)
		{
			this._openApiDiff = openApiDiff;
		}

		public ChangedHeadersBO Diff(IDictionary<string, OpenApiHeader> left, IDictionary<string, OpenApiHeader> right,
			DiffContextBO context)
		{
			var headerMapDiff = MapKeyDiff<string, OpenApiHeader>.Diff(left, right);
			List<string> sharedHeaderKeys = headerMapDiff.SharedKey;

			var changed = new Dictionary<string, ChangedHeaderBO>();
			foreach (string headerKey in sharedHeaderKeys)
			{
				OpenApiHeader oldHeader = left[headerKey];
				OpenApiHeader newHeader = right[headerKey];
				ChangedHeaderBO changedHeaders = this._openApiDiff
					.HeaderDiff
					.Diff(oldHeader, newHeader, context);
				if (changedHeaders != null)
					changed.Add(headerKey, changedHeaders);
			}

			return ChangedUtils.IsChanged(
				new ChangedHeadersBO(left, right, context)
				{
					Increased = headerMapDiff.Increased,
					Missing = headerMapDiff.Missing,
					Changed = changed
				});
		}
	}
}