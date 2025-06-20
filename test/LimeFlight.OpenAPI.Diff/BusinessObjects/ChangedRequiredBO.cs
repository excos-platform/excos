﻿using System.Collections.Generic;
using LimeFlight.OpenAPI.Diff.Enums;
using LimeFlight.OpenAPI.Diff.Extensions;

namespace LimeFlight.OpenAPI.Diff.BusinessObjects
{
	public class ChangedRequiredBO : ChangedListBO<string>
	{
		public ChangedRequiredBO(IList<string> oldValue, IList<string> newValue, DiffContextBO context) : base(oldValue,
			newValue, context)
		{
		}

		protected override ChangedElementTypeEnum GetElementType() => ChangedElementTypeEnum.Required;

		public override DiffResultBO IsItemsChanged()
		{
			if (this.Context.IsRequest && this.Increased.IsNullOrEmpty()
				|| this.Context.IsResponse && this.Missing.IsNullOrEmpty())
				return new DiffResultBO(DiffResultEnum.Compatible);
			return new DiffResultBO(DiffResultEnum.Incompatible);
		}
	}
}