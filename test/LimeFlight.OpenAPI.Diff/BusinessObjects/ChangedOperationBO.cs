using System.Collections.Generic;
using System.Linq;
using LimeFlight.OpenAPI.Diff.Enums;
using Microsoft.OpenApi.Models;

namespace LimeFlight.OpenAPI.Diff.BusinessObjects
{
	public class ChangedOperationBO : ComposedChangedBO
	{
		public ChangedOperationBO(string pathUrl, OperationType httpMethod, OpenApiOperation oldOperation,
			OpenApiOperation newOperation)
		{
			this.PathUrl = pathUrl;
			this.HttpMethod = httpMethod;
			this.OldOperation = oldOperation;
			this.NewOperation = newOperation;
		}

		public OpenApiOperation OldOperation { get; }
		public OpenApiOperation NewOperation { get; }

		public OperationType HttpMethod { get; }
		public string PathUrl { get; }
		public ChangedMetadataBO Summary { get; set; }
		public ChangedMetadataBO Description { get; set; }
		public bool IsDeprecated { get; set; }
		public ChangedParametersBO Parameters { get; set; }
		public ChangedRequestBodyBO RequestBody { get; set; }
		public ChangedAPIResponseBO APIResponses { get; set; }
		public ChangedSecurityRequirementsBO SecurityRequirements { get; set; }
		public ChangedExtensionsBO Extensions { get; set; }

		protected override ChangedElementTypeEnum GetElementType()
		{
			return ChangedElementTypeEnum.Operation;
		}

		public EndpointBO ConvertToEndpoint()
		{
			var endpoint = new EndpointBO
			{
				PathUrl = this.PathUrl,
				Method = this.HttpMethod,
				Summary = this.NewOperation.Summary,
				Operation = this.NewOperation
			};
			return endpoint;
		}

		public override List<(string Identifier, ChangedBO Change)> GetChangedElements()
		{
			return new List<(string Identifier, ChangedBO Change)>
				{
					("Summary", this.Summary),
					("Description", this.Description),
					("Parameters", this.Parameters),
					("RequestBody", this.RequestBody),
					("Responses", this.APIResponses),
					("SecurityRequirements", this.SecurityRequirements),
					(null, this.Extensions)
				}
				.Where(x => x.Change != null).ToList();
		}

		public override DiffResultBO IsCoreChanged()
		{
			if (this.IsDeprecated) return new DiffResultBO(DiffResultEnum.Compatible);
			return new DiffResultBO(DiffResultEnum.NoChanges);
		}

		protected override List<ChangedInfoBO> GetCoreChanges()
		{
			var returnList = new List<ChangedInfoBO>();
			ChangedElementTypeEnum elementType = this.GetElementType();
			const TypeEnum changeType = TypeEnum.Changed;

			if (this.IsDeprecated)
				returnList.Add(new ChangedInfoBO(elementType, changeType, "Deprecation",
					this.OldOperation?.Deprecated.ToString(), this.NewOperation?.Deprecated.ToString()));

			return returnList;
		}

		public DiffResultBO ResultApiResponses()
		{
			return Result(this.APIResponses);
		}

		public DiffResultBO ResultRequestBody()
		{
			return this.RequestBody == null ? new DiffResultBO(DiffResultEnum.NoChanges) : this.RequestBody.IsChanged();
		}
	}
}