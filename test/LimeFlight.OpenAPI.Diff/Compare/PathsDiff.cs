using System;
using System.Collections.Generic;
using System.Linq;
using LimeFlight.OpenAPI.Diff.BusinessObjects;
using LimeFlight.OpenAPI.Diff.Extensions;
using LimeFlight.OpenAPI.Diff.Utils;
using Microsoft.OpenApi.Models;

namespace LimeFlight.OpenAPI.Diff.Compare
{
	public class PathsDiff
	{
		private readonly OpenApiDiff _openApiDiff;

		public PathsDiff(OpenApiDiff openApiDiff)
		{
			this._openApiDiff = openApiDiff;
		}

		public ChangedPathsBO Diff(Dictionary<string, OpenApiPathItem> left, Dictionary<string, OpenApiPathItem> right)
		{
			var changedPaths = new ChangedPathsBO(left, right);

			foreach ((string key, OpenApiPathItem value) in right) changedPaths.Increased.Add(key, value);

			foreach ((string key, OpenApiPathItem value) in left)
			{
				string template = key.NormalizePath();
				string result = right.Keys.FirstOrDefault(x => x.NormalizePath() == template);

				if (result != null)
				{
					if (!changedPaths.Increased.ContainsKey(result))
						throw new ArgumentException($"Two path items have the same signature: {template}");
					OpenApiPathItem rightPath = changedPaths.Increased[result];
					changedPaths.Increased.Remove(result);
					var paramsDict = new Dictionary<string, string>();
					if (key != result)
					{
						List<string> oldParams = key.ExtractParametersFromPath();
						List<string> newParams = result.ExtractParametersFromPath();
						for (int i = oldParams.Count - 1; i >= 0; i--) paramsDict.Add(oldParams[i], newParams[i]);
					}

					var context = new DiffContextBO
					{
						URL = key,
						Parameters = paramsDict
					};

					ChangedPathBO diff = this._openApiDiff
						.PathDiff
						.Diff(value, rightPath, context);

					if (diff != null)
						changedPaths.Changed.Add(result, diff);
				}
				else
				{
					changedPaths.Missing.Add(key, value);
				}
			}

			return ChangedUtils.IsChanged(changedPaths);
		}

		public static OpenApiPaths ValOrEmpty(OpenApiPaths path)
		{
			return path ?? new OpenApiPaths();
		}
	}
}