using System;
using System.Collections.Generic;
using System.Linq;
using LimeFlight.OpenAPI.Diff.Enums;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;

namespace LimeFlight.OpenAPI.Diff.Utils
{
	public class RefPointer<T>
	{
		public const string BaseRef = "#/components/";
		private readonly RefTypeEnum _refType;

		public RefPointer(RefTypeEnum refType)
		{
			this._refType = refType;
		}

		public T ResolveRef(OpenApiComponents components, T t, string reference)
		{
			if (reference != null)
			{
				string refName = this.GetRefName(reference);
				Dictionary<string, T> maps = this.GetMap(components);
				maps.TryGetValue(refName, out T result);
				if (result == null)
				{
					var caseInsensitiveDictionary = new Dictionary<string, T>(maps, StringComparer.OrdinalIgnoreCase);
					if (caseInsensitiveDictionary.TryGetValue(refName, out T insensitiveValue))
						throw new Exception(
							$"Reference case sensitive error. {refName} is not equal to {caseInsensitiveDictionary.First(x => x.Value.Equals(insensitiveValue)).Key}");

					throw new AggregateException($"ref '{reference}' doesn't exist.");
				}

				return result;
			}

			return t;
		}

		private Dictionary<string, T> GetMap(OpenApiComponents components)
		{
			switch (this._refType)
			{
				case RefTypeEnum.RequestBodies:
					return (Dictionary<string, T>)components.RequestBodies;
				case RefTypeEnum.Responses:
					return (Dictionary<string, T>)components.Responses;
				case RefTypeEnum.Parameters:
					return (Dictionary<string, T>)components.Parameters;
				case RefTypeEnum.Schemas:
					return (Dictionary<string, T>)components.Schemas;
				case RefTypeEnum.Headers:
					return (Dictionary<string, T>)components.Headers;
				case RefTypeEnum.SecuritySchemes:
					return (Dictionary<string, T>)components.SecuritySchemes;
				default:
					throw new ArgumentOutOfRangeException("Not mapped for refType: " + this._refType);
			}
		}

		public string GetRefName(string reference)
		{
			if (reference == null) return null;
			if (this._refType == RefTypeEnum.SecuritySchemes) return reference;

			string baseRef = GetBaseRefForType(this._refType.GetDisplayName());
			if (!reference.StartsWith(baseRef, StringComparison.CurrentCultureIgnoreCase))
				throw new AggregateException("Invalid ref: " + reference);
			return reference.Substring(baseRef.Length);
		}

		private static string GetBaseRefForType(string type)
		{
			return $"{BaseRef}{type}/";
		}
	}
}