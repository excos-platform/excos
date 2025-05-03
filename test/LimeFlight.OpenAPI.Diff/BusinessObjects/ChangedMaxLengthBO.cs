using System.Collections.Generic;
using LimeFlight.OpenAPI.Diff.Enums;

namespace LimeFlight.OpenAPI.Diff.BusinessObjects
{
	public class ChangedMaxLengthBO : ChangedBO
	{
		private readonly DiffContextBO _context;
		private readonly int? _newValue;

		private readonly int? _oldValue;

		public ChangedMaxLengthBO(int? oldValue, int? newValue, DiffContextBO context)
		{
			this._oldValue = oldValue;
			this._newValue = newValue;
			this._context = context;
		}

		protected override ChangedElementTypeEnum GetElementType()
		{
			return ChangedElementTypeEnum.MaxLength;
		}

		public override DiffResultBO IsChanged()
		{
			if (this._oldValue == this._newValue) return new DiffResultBO(DiffResultEnum.NoChanges);
			if (this._context.IsRequest && (this._newValue == null || this._oldValue != null && this._oldValue <= this._newValue)
				|| this._context.IsResponse && (this._oldValue == null || this._newValue != null && this._newValue <= this._oldValue))
				return new DiffResultBO(DiffResultEnum.Compatible);
			return new DiffResultBO(DiffResultEnum.Incompatible);
		}

		protected override List<ChangedInfoBO> GetCoreChanges()
		{
			var returnList = new List<ChangedInfoBO>();
			var elementType = this.GetElementType();
			const TypeEnum changeType = TypeEnum.Changed;

			if (this._oldValue != this._newValue)
				returnList.Add(new ChangedInfoBO(elementType, changeType, this._context.GetDiffContextElementType(),
					this._oldValue?.ToString(), this._newValue?.ToString()));

			return returnList;
		}
	}
}