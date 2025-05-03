﻿using System.Collections.Generic;
using LimeFlight.OpenAPI.Diff.Enums;
using LimeFlight.OpenAPI.Diff.Extensions;

namespace LimeFlight.OpenAPI.Diff.BusinessObjects
{
	public class ChangedEnumBO : ChangedListBO<string>
	{
		public ChangedEnumBO(IList<string> oldValue, IList<string> newValue, DiffContextBO context) : base(oldValue,
			newValue, context)
		{
		}

		protected override ChangedElementTypeEnum GetElementType()
		{
			return ChangedElementTypeEnum.Enum;
		}

		public override DiffResultBO IsItemsChanged()
		{
			if (this.Context.IsRequest && this.Missing.IsNullOrEmpty()
				|| this.Context.IsResponse && this.Increased.IsNullOrEmpty())
				return new DiffResultBO(DiffResultEnum.Compatible);
			return new DiffResultBO(DiffResultEnum.Incompatible);
		}
	}
}