﻿using LimeFlight.OpenAPI.Diff.BusinessObjects;
using LimeFlight.OpenAPI.Diff.Utils;
using Microsoft.OpenApi.Models;

namespace LimeFlight.OpenAPI.Diff.Compare
{
	public class PathDiff
	{
		private readonly OpenApiDiff _openApiDiff;

		public PathDiff(OpenApiDiff openApiDiff)
		{
			this._openApiDiff = openApiDiff;
		}

		public ChangedPathBO Diff(OpenApiPathItem left, OpenApiPathItem right, DiffContextBO context)
		{
			var oldOperationMap = left.Operations;
			var newOperationMap = right.Operations;
			var operationsDiff =
				MapKeyDiff<OperationType, OpenApiOperation>.Diff(oldOperationMap, newOperationMap);
			var sharedMethods = operationsDiff.SharedKey;
			var changedPath = new ChangedPathBO(context.URL, left, right, context)
			{
				Increased = operationsDiff.Increased,
				Missing = operationsDiff.Missing
			};
			foreach (var operationType in sharedMethods)
			{
				var oldOperation = oldOperationMap[operationType];
				var newOperation = newOperationMap[operationType];

				var diff = this._openApiDiff
					.OperationDiff
					.Diff(oldOperation, newOperation, context.CopyWithMethod(operationType));

				if (diff != null)
					changedPath.Changed.Add(diff);
			}

			changedPath.Extensions = this._openApiDiff
				.ExtensionsDiff
				.Diff(left.Extensions, right.Extensions, context);

			return ChangedUtils.IsChanged(changedPath);
		}
	}
}