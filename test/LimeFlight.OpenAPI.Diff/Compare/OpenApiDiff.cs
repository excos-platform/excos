using System;
using System.Collections.Generic;
using System.Linq;
using LimeFlight.OpenAPI.Diff.BusinessObjects;
using LimeFlight.OpenAPI.Diff.Extensions;
using LimeFlight.OpenAPI.Diff.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace LimeFlight.OpenAPI.Diff.Compare
{
	public class OpenApiDiff
	{
		private readonly ILogger _logger;

		public OpenApiDiff(OpenApiDocument oldSpecOpenApi, string oldSpecIdentifier, OpenApiDocument newSpecOpenApi,
			string newSpecIdentifier, IEnumerable<IExtensionDiff> extensions, ILogger logger)
		{
			this._logger = logger;
			this.OldSpecOpenApi = oldSpecOpenApi;
			this.NewSpecOpenApi = newSpecOpenApi;
			this.OldIdentifier = oldSpecIdentifier;
			this.NewIdentifier = newSpecIdentifier;

			if (null == oldSpecOpenApi || null == newSpecOpenApi)
				throw new Exception("one of the old or new object is null");

			this.InitializeFields(extensions);
		}

		public string OldIdentifier { get; }
		public string NewIdentifier { get; }

		public PathsDiff PathsDiff { get; set; }
		public PathDiff PathDiff { get; set; }
		public SchemaDiff SchemaDiff { get; set; }
		public ContentDiff ContentDiff { get; set; }
		public ParametersDiff ParametersDiff { get; set; }
		public ParameterDiff ParameterDiff { get; set; }
		public RequestBodyDiff RequestBodyDiff { get; set; }
		public ResponseDiff ResponseDiff { get; set; }
		public HeadersDiff HeadersDiff { get; set; }
		public HeaderDiff HeaderDiff { get; set; }
		public ApiResponseDiff APIResponseDiff { get; set; }
		public OperationDiff OperationDiff { get; set; }
		public SecurityRequirementsDiff SecurityRequirementsDiff { get; set; }
		public SecurityRequirementDiff SecurityRequirementDiff { get; set; }
		public SecuritySchemeDiff SecuritySchemeDiff { get; set; }
		public OAuthFlowsDiff OAuthFlowsDiff { get; set; }
		public OAuthFlowDiff OAuthFlowDiff { get; set; }
		public ExtensionsDiff ExtensionsDiff { get; set; }
		public MetadataDiff MetadataDiff { get; set; }
		public OpenApiDocument OldSpecOpenApi { get; set; }
		public OpenApiDocument NewSpecOpenApi { get; set; }
		public List<EndpointBO> NewEndpoints { get; set; }
		public List<EndpointBO> MissingEndpoints { get; set; }
		public List<ChangedOperationBO> ChangedOperations { get; set; }
		public ChangedExtensionsBO ChangedExtensions { get; set; }

		public static ChangedOpenApiBO Compare(OpenApiDocument oldSpecOpenApi, string oldSpecIdentifier,
			OpenApiDocument newSpecOpenApi, string newSpecIdentifier, IEnumerable<IExtensionDiff> extensions,
			ILogger logger)
		{
			return new OpenApiDiff(oldSpecOpenApi, oldSpecIdentifier, newSpecOpenApi, newSpecIdentifier, extensions,
				logger).Compare();
		}

		private void InitializeFields(IEnumerable<IExtensionDiff> extensions)
		{
			this.PathsDiff = new PathsDiff(this);
			this.PathDiff = new PathDiff(this);
			this.SchemaDiff = new SchemaDiff(this);
			this.ContentDiff = new ContentDiff(this);
			this.ParametersDiff = new ParametersDiff(this);
			this.ParameterDiff = new ParameterDiff(this);
			this.RequestBodyDiff = new RequestBodyDiff(this);
			this.ResponseDiff = new ResponseDiff(this);
			this.HeadersDiff = new HeadersDiff(this);
			this.HeaderDiff = new HeaderDiff(this);
			this.APIResponseDiff = new ApiResponseDiff(this);
			this.OperationDiff = new OperationDiff(this);
			this.SecurityRequirementsDiff = new SecurityRequirementsDiff(this);
			this.SecurityRequirementDiff = new SecurityRequirementDiff(this);
			this.SecuritySchemeDiff = new SecuritySchemeDiff(this);
			this.OAuthFlowsDiff = new OAuthFlowsDiff(this);
			this.OAuthFlowDiff = new OAuthFlowDiff(this);
			this.ExtensionsDiff = new ExtensionsDiff(this, extensions);
			this.MetadataDiff = new MetadataDiff(this);
		}

		private ChangedOpenApiBO Compare()
		{
			PreProcess(this.OldSpecOpenApi);
			PreProcess(this.NewSpecOpenApi);
			var paths =
				this.PathsDiff.Diff(PathsDiff.ValOrEmpty(this.OldSpecOpenApi.Paths), PathsDiff.ValOrEmpty(this.NewSpecOpenApi.Paths));
			this.NewEndpoints = new List<EndpointBO>();
			this.MissingEndpoints = new List<EndpointBO>();
			this.ChangedOperations = new List<ChangedOperationBO>();

			if (paths != null)
			{
				this.NewEndpoints = EndpointUtils.ConvertToEndpointList<EndpointBO>(paths.Increased);
				this.MissingEndpoints = EndpointUtils.ConvertToEndpointList<EndpointBO>(paths.Missing);
				foreach (var (key, value) in paths.Changed)
				{
					this.NewEndpoints.AddRange(EndpointUtils.ConvertToEndpoints<EndpointBO>(key, value.Increased));
					this.MissingEndpoints.AddRange(EndpointUtils.ConvertToEndpoints<EndpointBO>(key, value.Missing));
					this.ChangedOperations.AddRange(value.Changed);
				}
			}

			var diff = this.ExtensionsDiff
				.Diff(this.OldSpecOpenApi.Extensions, this.NewSpecOpenApi.Extensions);

			if (diff != null)
				this.ChangedExtensions = diff;
			return this.GetChangedOpenApi();
		}

		private static void PreProcess(OpenApiDocument openApi)
		{
			var securityRequirements = openApi.SecurityRequirements;

			if (securityRequirements != null)
			{
				var distinctSecurityRequirements =
					securityRequirements.Distinct().ToList();
				var paths = openApi.Paths;
				if (paths != null)
					foreach (var openApiPathItem in paths.Values)
					{
						var operationsWithSecurity = openApiPathItem
							.Operations
							.Values
							.Where(x => !x.Security.IsNullOrEmpty());
						foreach (var openApiOperation in operationsWithSecurity)
							openApiOperation.Security = openApiOperation.Security.Distinct().ToList();
						var operationsWithoutSecurity = openApiPathItem
							.Operations
							.Values
							.Where(x => x.Security.IsNullOrEmpty());
						foreach (var openApiOperation in operationsWithoutSecurity)
							openApiOperation.Security = distinctSecurityRequirements;
					}

				openApi.SecurityRequirements = null;
			}
		}

		private ChangedOpenApiBO GetChangedOpenApi()
		{
			return new ChangedOpenApiBO(this.OldIdentifier, this.NewIdentifier)
			{
				MissingEndpoints = this.MissingEndpoints,
				NewEndpoints = this.NewEndpoints,
				NewSpecOpenApi = this.NewSpecOpenApi,
				OldSpecOpenApi = this.OldSpecOpenApi,
				ChangedOperations = this.ChangedOperations,
				ChangedExtensions = this.ChangedExtensions
			};
		}
	}
}