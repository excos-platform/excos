using System.Collections.Generic;
using LimeFlight.OpenAPI.Diff.Enums;
using LimeFlight.OpenAPI.Diff.Extensions;
using Microsoft.OpenApi.Extensions;

namespace LimeFlight.OpenAPI.Diff.BusinessObjects
{
	public abstract class ChangedBO
	{
		protected abstract ChangedElementTypeEnum GetElementType();

		protected string GetIdentifier(string identifier)
		{
			return identifier ?? this.GetElementType().GetDisplayName();
		}

		public abstract DiffResultBO IsChanged();

		public virtual DiffResultBO IsCoreChanged()
		{
			return this.IsChanged();
		}

		protected abstract List<ChangedInfoBO> GetCoreChanges();

		public ChangedInfosBO GetCoreChangeInfo(string identifier, List<string> parentPath = null)
		{
			DiffResultBO isChanged = this.IsCoreChanged();
			var newPath = new List<string>();

			if (!parentPath.IsNullOrEmpty())
				newPath = new List<string>(parentPath);

			newPath.Add(this.GetIdentifier(identifier));

			var result = new ChangedInfosBO
			{
				Path = newPath,
				ChangeType = isChanged
			};

			if (isChanged.IsUnchanged())
				return result;

			result.Changes = this.GetCoreChanges();
			return result;
		}

		public virtual List<ChangedInfosBO> GetAllChangeInfoFlat(string identifier, List<string> parentPath = null)
		{
			return new List<ChangedInfosBO>
			{
				this.GetCoreChangeInfo(identifier, parentPath)
			};
		}

		public static DiffResultBO Result(ChangedBO changed)
		{
			return changed?.IsChanged() ?? new DiffResultBO(DiffResultEnum.NoChanges);
		}

		public bool IsCompatible()
		{
			return this.IsChanged().IsCompatible();
		}

		public bool IsIncompatible()
		{
			return this.IsChanged().IsIncompatible();
		}

		public bool IsUnchanged()
		{
			return this.IsChanged().IsUnchanged();
		}

		public bool IsDifferent()
		{
			return this.IsChanged().IsDifferent();
		}
	}
}