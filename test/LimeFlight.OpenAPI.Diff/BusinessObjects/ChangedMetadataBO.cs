using System.Collections.Generic;
using LimeFlight.OpenAPI.Diff.Enums;

namespace LimeFlight.OpenAPI.Diff.BusinessObjects
{
	public class ChangedMetadataBO : ChangedBO
	{
		public ChangedMetadataBO(string left, string right)
		{
			this.Left = left;
			this.Right = right;
		}

		public string Left { get; }
		public string Right { get; }

		protected override ChangedElementTypeEnum GetElementType()
		{
			return ChangedElementTypeEnum.Metadata;
		}

		public override DiffResultBO IsChanged()
		{
			return this.Left == this.Right
				? new DiffResultBO(DiffResultEnum.NoChanges)
				: new DiffResultBO(DiffResultEnum.Metadata);
		}

		protected override List<ChangedInfoBO> GetCoreChanges()
		{
			var returnList = new List<ChangedInfoBO>();
			var elementType = this.GetElementType();
			const TypeEnum changeType = TypeEnum.Changed;

			if (this.Left != this.Right)
				returnList.Add(new ChangedInfoBO(elementType, changeType, "Value", this.Left, this.Right));

			return returnList;
		}
	}
}