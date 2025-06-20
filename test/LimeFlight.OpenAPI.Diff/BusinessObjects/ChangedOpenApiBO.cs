using System.Collections.Generic;
using System.Linq;
using LimeFlight.OpenAPI.Diff.Enums;
using LimeFlight.OpenAPI.Diff.Extensions;
using Microsoft.OpenApi.Models;

namespace LimeFlight.OpenAPI.Diff.BusinessObjects
{
	public class ChangedOpenApiBO : ComposedChangedBO
	{
		public ChangedOpenApiBO(string oldSpecIdentifier, string newSpecIdentifier)
		{
			this.NewEndpoints = new List<EndpointBO>();
			this.MissingEndpoints = new List<EndpointBO>();
			this.ChangedOperations = new List<ChangedOperationBO>();
			this.OldSpecIdentifier = oldSpecIdentifier;
			this.NewSpecIdentifier = newSpecIdentifier;
		}

		public string OldSpecIdentifier { get; set; }
		public string NewSpecIdentifier { get; set; }
		public OpenApiDocument OldSpecOpenApi { get; set; }
		public OpenApiDocument NewSpecOpenApi { get; set; }
		public List<EndpointBO> NewEndpoints { get; set; }
		public List<EndpointBO> MissingEndpoints { get; set; }
		public List<ChangedOperationBO> ChangedOperations { get; set; }
		public ChangedExtensionsBO ChangedExtensions { get; set; }

		protected override ChangedElementTypeEnum GetElementType()
		{
			return ChangedElementTypeEnum.OpenApi;
		}

		public List<EndpointBO> GetDeprecatedEndpoints()
		{
			return this.ChangedOperations
				.Where(x => x.IsDeprecated)
				.Select(x => x.ConvertToEndpoint())
				.ToList();
		}

		public override List<(string Identifier, ChangedBO Change)> GetChangedElements()
		{
			return new List<(string Identifier, ChangedBO Change)>(
					this.ChangedOperations.Select(x => (x.PathUrl, (ChangedBO)x))
				)
				{
					(null, this.ChangedExtensions)
				}
				.Where(x => x.Change != null).ToList();
		}

		public override DiffResultBO IsCoreChanged()
		{
			if (this.NewEndpoints.IsNullOrEmpty() && this.MissingEndpoints.IsNullOrEmpty())
				return new DiffResultBO(DiffResultEnum.NoChanges);
			if (this.MissingEndpoints.IsNullOrEmpty()) return new DiffResultBO(DiffResultEnum.Compatible);
			return new DiffResultBO(DiffResultEnum.Incompatible);
		}

		protected override List<ChangedInfoBO> GetCoreChanges()
		{
			return this.GetCoreChangeInfosOfComposed(this.NewEndpoints, this.MissingEndpoints, x => x.PathUrl);
		}
	}
}