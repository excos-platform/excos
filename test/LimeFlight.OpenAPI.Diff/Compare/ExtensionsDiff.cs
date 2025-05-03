using System;
using System.Collections.Generic;
using System.Linq;
using LimeFlight.OpenAPI.Diff.BusinessObjects;
using LimeFlight.OpenAPI.Diff.Enums;
using LimeFlight.OpenAPI.Diff.Extensions;
using LimeFlight.OpenAPI.Diff.Utils;
using Microsoft.OpenApi.Interfaces;

namespace LimeFlight.OpenAPI.Diff.Compare
{
	public class ExtensionsDiff
	{
		private readonly IEnumerable<IExtensionDiff> _extensions;
		private readonly OpenApiDiff _openApiDiff;

		public ExtensionsDiff(OpenApiDiff openApiDiff, IEnumerable<IExtensionDiff> extensions)
		{
			this._openApiDiff = openApiDiff;
			this._extensions = extensions;
		}

		public bool IsParentApplicable(TypeEnum type, object parent, IDictionary<string, IOpenApiExtension> extensions,
			DiffContextBO context)
		{
			if (extensions.IsNullOrEmpty())
				return true;

			return extensions.Select(x => this.ExecuteExtension(x.Key, y => y
					.IsParentApplicable(type, parent, x.Value, context)))
				.All(x => x);
		}

		public IExtensionDiff GetExtensionDiff(string name)
		{
			if (this._extensions.IsNullOrEmpty())
				return null;

			return this._extensions.FirstOrDefault(x => $"x-{x.GetName()}" == name);
		}

		public T ExecuteExtension<T>(string name, Func<ExtensionDiff, T> predicate)
		{
			if (this._extensions.IsNullOrEmpty())
				return default;

			return predicate(this.GetExtensionDiff(name).SetOpenApiDiff(this._openApiDiff));
		}

		public ChangedExtensionsBO Diff(IDictionary<string, IOpenApiExtension> left,
			IDictionary<string, IOpenApiExtension> right)
		{
			return this.Diff(left, right, null);
		}

		public ChangedExtensionsBO Diff(IDictionary<string, IOpenApiExtension> left,
			IDictionary<string, IOpenApiExtension> right, DiffContextBO context)
		{
			left = ((Dictionary<string, IOpenApiExtension>)left).CopyDictionary();
			right = ((Dictionary<string, IOpenApiExtension>)right).CopyDictionary();
			var changedExtensions = new ChangedExtensionsBO((Dictionary<string, IOpenApiExtension>)left,
				((Dictionary<string, IOpenApiExtension>)right).CopyDictionary(), context);
			foreach ((string key, IOpenApiExtension value) in left)
				if (right.ContainsKey(key))
				{
					IOpenApiExtension rightValue = right[key];
					right.Remove(key);
					ChangedBO changed = this.ExecuteExtensionDiff(key, ChangeBO<object>.Changed(value, rightValue), context);
					if (changed?.IsDifferent() ?? false)
						changedExtensions.Changed.Add(key, changed);
				}
				else
				{
					ChangedBO changed = this.ExecuteExtensionDiff(key, ChangeBO<object>.Removed(value), context);
					if (changed?.IsDifferent() ?? false)
						changedExtensions.Missing.Add(key, changed);
				}

			foreach ((string key, IOpenApiExtension value) in right)
			{
				ChangedBO changed = this.ExecuteExtensionDiff(key, ChangeBO<object>.Added(value), context);
				if (changed?.IsDifferent() ?? false)
					changedExtensions.Increased.Add(key, changed);
			}

			return ChangedUtils.IsChanged(changedExtensions);
		}

		private ChangedBO ExecuteExtensionDiff<T>(string name, ChangeBO<T> change, DiffContextBO context)
			where T : class
		{
			return this.ExecuteExtension(name, x => x.Diff(change, context));
		}
	}
}