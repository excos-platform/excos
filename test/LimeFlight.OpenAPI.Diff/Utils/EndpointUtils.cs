using System.Collections.Generic;
using LimeFlight.OpenAPI.Diff.BusinessObjects;
using Microsoft.OpenApi.Models;

namespace LimeFlight.OpenAPI.Diff.Utils
{
	public class EndpointUtils
	{
		public static List<T> ConvertToEndpoints<T>(string pathUrl, Dictionary<OperationType, OpenApiOperation> dict)
			where T : EndpointBO, new()
		{
			var endpoints = new List<T>();
			if (dict == null) return endpoints;
			foreach ((OperationType key, OpenApiOperation value) in dict)
			{
				T endpoint = ConvertToEndpoint<T>(pathUrl, key, value);
				endpoints.Add(endpoint);
			}

			return endpoints;
		}

		public static T ConvertToEndpoint<T>(string pathUrl, OperationType httpMethod, OpenApiOperation operation)
			where T : EndpointBO, new()
		{
			var endpoint = new T
			{
				PathUrl = pathUrl,
				Method = httpMethod,
				Summary = operation.Summary,
				Operation = operation
			};
			return endpoint;
		}

		public static List<T> ConvertToEndpointList<T>(Dictionary<string, OpenApiPathItem> dict)
			where T : EndpointBO, new()
		{
			var endpoints = new List<T>();
			if (dict == null) return endpoints;

			foreach ((string key, OpenApiPathItem value) in dict)
			{
				IDictionary<OperationType, OpenApiOperation> operationMap = value.Operations;
				foreach ((OperationType operationType, OpenApiOperation openApiOperation) in operationMap)
				{
					var endpoint = new T
					{
						PathUrl = key,
						Method = operationType,
						Summary = openApiOperation.Summary,
						Path = value,
						Operation = openApiOperation
					};
					endpoints.Add(endpoint);
				}
			}

			return endpoints;
		}
	}
}