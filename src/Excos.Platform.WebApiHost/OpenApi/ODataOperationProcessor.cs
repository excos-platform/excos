// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace Excos.Platform.WebApiHost.OpenApi;

public class ODataOperationProcessor : IOperationProcessor
{
	public bool Process(OperationProcessorContext context)
	{
		IEnumerable<NSwag.OpenApiOperationDescription> operationsToModify = context.AllOperationDescriptions
			.Where(op => op.Path.StartsWith("/api"));

		foreach (NSwag.OpenApiOperationDescription? operation in operationsToModify)
		{
			if (operation.Path.EndsWith("$count") && !operation.Operation.OperationId.EndsWith("Count"))
			{
				operation.Operation.OperationId += "Count";
			}
			if (operation.Path.Contains("{key}") && !operation.Operation.OperationId.EndsWith("ByKey"))
			{
				operation.Operation.OperationId += "ByKey";
			}
		}

		return true;
	}
}
