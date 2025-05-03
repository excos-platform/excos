using LimeFlight.OpenAPI.Diff.Enums;

namespace LimeFlight.OpenAPI.Diff.BusinessObjects
{
	public class DiffResultBO
	{
		public readonly DiffResultEnum DiffResult;

		public DiffResultBO(DiffResultEnum diffResult)
		{
			this.DiffResult = diffResult;
		}

		public bool IsUnchanged()
		{
			return this.DiffResult == 0;
		}

		public bool IsDifferent()
		{
			return this.DiffResult > 0;
		}

		public bool IsIncompatible()
		{
			return (int)this.DiffResult > 2;
		}

		public bool IsCompatible()
		{
			return (int)this.DiffResult <= 2;
		}

		public bool IsMetaChanged()
		{
			return (int)this.DiffResult == 1;
		}
	}
}