using System.Collections.Generic;
using LimeFlight.OpenAPI.Diff.Enums;

namespace LimeFlight.OpenAPI.Diff.BusinessObjects
{
	public class ChangedWriteOnlyBO : ChangedBO
	{
		private readonly DiffContextBO _context;
		private readonly bool _newValue;
		private readonly bool _oldValue;

		public ChangedWriteOnlyBO(bool? oldValue, bool? newValue, DiffContextBO context)
		{
			this._context = context;
			this._oldValue = oldValue ?? false;
			this._newValue = newValue ?? false;
		}

		protected override ChangedElementTypeEnum GetElementType() => ChangedElementTypeEnum.WriteOnly;

		public override DiffResultBO IsChanged()
		{
			if (this._oldValue == this._newValue) return new DiffResultBO(DiffResultEnum.NoChanges);
			if (this._context.IsRequest) return new DiffResultBO(DiffResultEnum.Compatible);
			if (this._context.IsResponse)
			{
				if (this._newValue) return new DiffResultBO(DiffResultEnum.Incompatible);

				return this._context.IsRequired
					? new DiffResultBO(DiffResultEnum.Incompatible)
					: new DiffResultBO(DiffResultEnum.Compatible);
			}

			return new DiffResultBO(DiffResultEnum.Unknown);
		}

		protected override List<ChangedInfoBO> GetCoreChanges()
		{
			var returnList = new List<ChangedInfoBO>();
			ChangedElementTypeEnum elementType = this.GetElementType();
			const TypeEnum changeType = TypeEnum.Changed;

			if (this._oldValue != this._newValue)
				returnList.Add(new ChangedInfoBO(elementType, changeType, this._context.GetDiffContextElementType(),
					this._oldValue.ToString(), this._newValue.ToString()));

			return returnList;
		}
	}
}