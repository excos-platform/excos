using System.Collections.Generic;
using LimeFlight.OpenAPI.Diff.BusinessObjects;

namespace LimeFlight.OpenAPI.Diff.Compare
{
	public abstract class ReferenceDiffCache<TC, TD>
		where TD : class
	{
		protected ReferenceDiffCache()
		{
			this.RefDiffMap = new Dictionary<CacheKey, TD>();
		}

		public Dictionary<CacheKey, TD> RefDiffMap { get; set; }

		protected abstract TD ComputeDiff(TC left, TC right, DiffContextBO context);

		public TD CachedDiff(
			TC left,
			TC right,
			string leftRef,
			string rightRef,
			DiffContextBO context)
		{
			bool areBothRefParameters = leftRef != null && rightRef != null;
			if (areBothRefParameters)
			{
				var key = new CacheKey(leftRef, rightRef, context);
				if (this.RefDiffMap.TryGetValue(key, out TD changedFromRef))
					return changedFromRef;

				this.RefDiffMap.Add(key, null);
				TD changed = this.ComputeDiff(left, right, context);
				this.RefDiffMap[key] = changed;
				return changed;
			}

			return this.ComputeDiff(left, right, context);
		}
	}
}