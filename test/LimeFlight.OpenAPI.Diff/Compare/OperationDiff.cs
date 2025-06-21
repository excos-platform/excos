using System.Collections.Generic;
using System.Linq;
using LimeFlight.OpenAPI.Diff.BusinessObjects;
using LimeFlight.OpenAPI.Diff.Extensions;
using LimeFlight.OpenAPI.Diff.Utils;
using Microsoft.OpenApi.Models;

namespace LimeFlight.OpenAPI.Diff.Compare
{
	public class OperationDiff
	{
		private readonly OpenApiDiff _openApiDiff;

		public OperationDiff(OpenApiDiff openApiDiff)
		{
			this._openApiDiff = openApiDiff;
		}

		public ChangedOperationBO Diff(
			OpenApiOperation oldOperation, OpenApiOperation newOperation, DiffContextBO context)
		{
			var changedOperation =
				new ChangedOperationBO(context.URL, context.Method, oldOperation, newOperation)
				{
					Summary = this._openApiDiff
						.MetadataDiff
						.Diff(oldOperation.Summary, newOperation.Summary, context),
					Description = this._openApiDiff
						.MetadataDiff
						.Diff(oldOperation.Description, newOperation.Description, context),
					IsDeprecated = !oldOperation.Deprecated && newOperation.Deprecated
				};

			if (oldOperation.RequestBody != null || newOperation.RequestBody != null)
				changedOperation.RequestBody = this._openApiDiff
					.RequestBodyDiff
					.Diff(
						oldOperation.RequestBody, newOperation.RequestBody, context.CopyAsRequest());

			ChangedParametersBO parametersDiff = this._openApiDiff
				.ParametersDiff
				.Diff(oldOperation.Parameters.ToList(), newOperation.Parameters.ToList(), context);

			if (parametersDiff != null)
			{
				this.RemovePathParameters(context.Parameters, parametersDiff);
				changedOperation.Parameters = parametersDiff;
			}


			if (oldOperation.Responses != null || newOperation.Responses != null)
			{
				ChangedAPIResponseBO diff = this._openApiDiff
					.APIResponseDiff
					.Diff(oldOperation.Responses, newOperation.Responses, context.CopyAsResponse());

				if (diff != null)
					changedOperation.APIResponses = diff;
			}

			if (oldOperation.Security != null || newOperation.Security != null)
			{
				ChangedSecurityRequirementsBO diff = this._openApiDiff
					.SecurityRequirementsDiff
					.Diff(oldOperation.Security, newOperation.Security, context);

				if (diff != null)
					changedOperation.SecurityRequirements = diff;
			}

			changedOperation.Extensions =
				this._openApiDiff
					.ExtensionsDiff
					.Diff(oldOperation.Extensions, newOperation.Extensions, context);

			return ChangedUtils.IsChanged(changedOperation);
		}

		public void RemovePathParameters(Dictionary<string, string> pathParameters, ChangedParametersBO parameters)
		{
			foreach ((string oldParam, string newParam) in pathParameters)
			{
				this.RemovePathParameter(oldParam, parameters.Missing);
				this.RemovePathParameter(newParam, parameters.Increased);
			}
		}

		public void RemovePathParameter(string name, List<OpenApiParameter> parameters)
		{
			OpenApiParameter openApiParameters = parameters
				.FirstOrDefault(x => x.In == ParameterLocation.Path && x.Name == name);
			if (!parameters.IsNullOrEmpty() && openApiParameters != null)
				parameters.Remove(openApiParameters);
		}
	}
}