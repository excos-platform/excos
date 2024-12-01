﻿// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Excos.Platform.WebApiHost.Utils
{
	public static class GuidExtensions
	{
		/// <summary>
		/// Creates a name-based UUID using the algorithm from RFC 4122 §4.3.
		/// </summary>
		/// <param name="namespaceId">The ID of the namespace.</param>
		/// <param name="name">The name (within that namespace).</param>
		/// <returns>A UUID derived from the namespace and name.</returns>
		public static Guid V5(this Guid namespaceId, string name)
		{
			const int version = 5;
			// convert the namespace UUID to network order (step 3)
			Span<byte> namespaceBytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref namespaceId, 1));
			SwapByteOrder(namespaceBytes);

			ReadOnlySpan<byte> nameBytes = MemoryMarshal.AsBytes(name.AsSpan());

			// compute the hash of the namespace ID concatenated with the name (step 4)
			Span<byte> source = stackalloc byte[namespaceBytes.Length + nameBytes.Length];
			namespaceBytes.CopyTo(source);
			nameBytes.CopyTo(source.Slice(namespaceBytes.Length));

			Span<byte> hash = stackalloc byte[SHA1.HashSizeInBytes];
			SHA1.TryHashData(source, hash, out _);

			// most bytes from the hash are copied straight to the bytes of the new GUID (steps 5-7, 9, 11-12)
			Span<byte> newGuid = stackalloc byte[16];
			hash.Slice(0, 16).CopyTo(newGuid);

			// set the four most significant bits (bits 12 through 15) of the time_hi_and_version field to the appropriate 4-bit version number from Section 4.1.3 (step 8)
			newGuid[6] = (byte)((newGuid[6] & 0x0F) | (version << 4));

			// set the two most significant bits (bits 6 and 7) of the clock_seq_hi_and_reserved to zero and one, respectively (step 10)
			newGuid[8] = (byte)((newGuid[8] & 0x3F) | 0x80);

			// convert the resulting UUID to local byte order (step 13)
			SwapByteOrder(newGuid);
			return new Guid(newGuid);
		}

		internal static void SwapByteOrder(Span<byte> guid)
		{
			SwapBytes(guid, 0, 3);
			SwapBytes(guid, 1, 2);
			SwapBytes(guid, 4, 5);
			SwapBytes(guid, 6, 7);
		}

		private static void SwapBytes(Span<byte> guid, int left, int right)
		{
			byte temp = guid[left];
			guid[left] = guid[right];
			guid[right] = temp;
		}
	}
}
