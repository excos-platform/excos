// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

namespace Excos.Testing.OpenTelemetry.Asserts
{
	[Serializable]
	internal class AssertException : Exception
	{
		public AssertException()
		{
		}

		public AssertException(string? message) : base(message)
		{
		}

		public AssertException(string? message, Exception? innerException) : base(message, innerException)
		{
		}
	}
}