﻿using System;
using System.Collections.Generic;
using System.IO;
using LimeFlight.OpenAPI.Diff.BusinessObjects;
using LimeFlight.OpenAPI.Diff.Compare;
using LimeFlight.OpenAPI.Diff.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;

namespace LimeFlight.OpenAPI.Diff
{
	public class OpenAPICompare : IOpenAPICompare
	{
		private readonly IEnumerable<IExtensionDiff> _extensions;
		private readonly ILogger<OpenAPICompare> _logger;

		public OpenAPICompare(ILogger<OpenAPICompare> logger, IEnumerable<IExtensionDiff> extensions)
		{
			this._logger = logger;
			this._extensions = extensions;
		}

		public ChangedOpenApiBO FromLocations(string oldLocation, string newLocation,
			OpenApiReaderSettings settings = null)
		{
			return this.FromLocations(oldLocation, Path.GetFileNameWithoutExtension(oldLocation), newLocation,
				Path.GetFileNameWithoutExtension(newLocation), settings);
		}

		public ChangedOpenApiBO FromSpecifications(OpenApiDocument oldSpec, string oldSpecIdentifier,
			OpenApiDocument newSpec, string newSpecIdentifier)
		{
			return OpenApiDiff.Compare(oldSpec, oldSpecIdentifier, newSpec, newSpecIdentifier, this._extensions, this._logger);
		}

		public ChangedOpenApiBO FromLocations(string oldLocation, string oldIdentifier, string newLocation,
			string newIdentifier, OpenApiReaderSettings settings = null)
		{
			return this.FromSpecifications(ReadLocation(oldLocation, settings: settings), oldIdentifier,
				ReadLocation(newLocation, settings: settings), newIdentifier);
		}

		private static OpenApiDocument ReadLocation(string location, List<OpenApiOAuthFlow> auths = null,
			OpenApiReaderSettings settings = null)
		{
			using var sr = new StreamReader(location);

			OpenApiDocument openAPIDoc = new OpenApiStreamReader(settings).Read(sr.BaseStream, out OpenApiDiagnostic diagnostic);
			if (!diagnostic.Errors.IsNullOrEmpty())
				throw new Exception($"Error reading file. Error: {string.Join(", ", diagnostic.Errors)}");

			return openAPIDoc;
		}
	}
}