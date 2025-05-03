﻿using System;
using System.Collections.Generic;
using System.Linq;
using LimeFlight.OpenAPI.Diff.Enums;

namespace LimeFlight.OpenAPI.Diff.BusinessObjects
{
	public abstract class ComposedChangedBO : ChangedBO
	{
		public abstract List<(string Identifier, ChangedBO Change)> GetChangedElements();

		public override List<ChangedInfosBO> GetAllChangeInfoFlat(string identifier, List<string> parentPath = null)
		{
			var coreChangeInfo = this.GetCoreChangeInfo(identifier, parentPath);
			var changedElements = this.GetChangedElements();
			var returnList = changedElements
				.SelectMany(x => x.Change.GetAllChangeInfoFlat(x.Identifier, coreChangeInfo.Path))
				.Where(x => !x.ChangeType.IsUnchanged())
				.OrderBy(x => x.Path.Count)
				.ToList();

			returnList.Add(coreChangeInfo);

			return returnList;
		}

		public override DiffResultBO IsChanged()
		{
			var elementsResultMax = this.GetChangedElements()
				.Where(x => x.Change != null)
				.Select(x => (int)x.Change.IsChanged().DiffResult)
				.DefaultIfEmpty(0)
				.Max();

			var elementsResult = new DiffResultBO((DiffResultEnum)elementsResultMax);

			return this.IsCoreChanged().DiffResult > elementsResult.DiffResult ? this.IsCoreChanged() : elementsResult;
		}

		protected List<ChangedInfoBO> GetCoreChangeInfosOfComposed<T>(List<T> increased, List<T> missing,
			Func<T, string> identifierSelector)
		{
			var returnList = new List<ChangedInfoBO>();
			var elementType = this.GetElementType();

			foreach (var listElement in increased)
				returnList.Add(ChangedInfoBO.ForAdded(elementType, identifierSelector(listElement)));

			foreach (var listElement in missing)
				returnList.Add(ChangedInfoBO.ForRemoved(elementType, identifierSelector(listElement)));
			return returnList;
		}
	}
}