// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Frozen;

namespace Files.App.Data.Models
{
	public struct ByteSize : IEquatable<ByteSize>, IComparable<ByteSize>
	{
		private static readonly FrozenDictionary<string, string> units = new Dictionary<string, string>
		{
			[ByteSizeLib.ByteSize.BitSymbol] = "ByteSymbol".ToLocalized(),
			[ByteSizeLib.ByteSize.ByteSymbol] = "ByteSymbol".ToLocalized(),
			[ByteSizeLib.ByteSize.KibiByteSymbol] = "KiloByteSymbol".ToLocalized(),
			[ByteSizeLib.ByteSize.MebiByteSymbol] = "MegaByteSymbol".ToLocalized(),
			[ByteSizeLib.ByteSize.GibiByteSymbol] = "GigaByteSymbol".ToLocalized(),
			[ByteSizeLib.ByteSize.TebiByteSymbol] = "TeraByteSymbol".ToLocalized(),
			[ByteSizeLib.ByteSize.PebiByteSymbol] = "PetaByteSymbol".ToLocalized(),
		}.ToFrozenDictionary();

		private readonly ByteSizeLib.ByteSize size;

		public static readonly ByteSize Zero = new(0L);

		public static readonly ByteSize MaxValue = new(long.MaxValue);

		public ulong Bytes
			=> (ulong)size.Bytes;

		public string ShortString
			=> $"{size.LargestWholeNumberBinaryValue:0.##} {units[size.LargestWholeNumberBinarySymbol]}";

		public string LongString
			=> $"{ShortString} ({size.Bytes:#,##0} {units[ByteSizeLib.ByteSize.ByteSymbol]})";

		public static ByteSize FromBytes(ulong bytes) => new(bytes);
		public static ByteSize FromKibiBytes(ulong kibiBytes) => new(kibiBytes * ByteSizeLib.ByteSize.BytesInKibiByte);
		public static ByteSize FromMebiBytes(ulong mebiBytes) => new(mebiBytes * ByteSizeLib.ByteSize.BytesInMebiByte);
		public static ByteSize FromGibiBytes(ulong gibiBytes) => new(gibiBytes * ByteSizeLib.ByteSize.BytesInGibiByte);
		public static ByteSize FromTebiBytes(ulong tebiBytes) => new(tebiBytes * ByteSizeLib.ByteSize.BytesInTebiByte);
		public static ByteSize FromPebiBytes(ulong pebiBytes) => new(pebiBytes * ByteSizeLib.ByteSize.BytesInPebiByte);

		public static explicit operator ulong(ByteSize size) => size.Bytes;
		public static implicit operator ByteSize(ulong size) => new(size);

		public static ByteSize operator +(ByteSize a, ByteSize b) => new(a.Bytes + b.Bytes);
		public static ByteSize operator -(ByteSize a, ByteSize b) => new(a.Bytes - b.Bytes);
		public static bool operator ==(ByteSize a, ByteSize b) => a.size == b.size;
		public static bool operator !=(ByteSize a, ByteSize b) => a.size != b.size;
		public static bool operator <(ByteSize a, ByteSize b) => a.size < b.size;
		public static bool operator >(ByteSize a, ByteSize b) => a.size > b.size;
		public static bool operator <=(ByteSize a, ByteSize b) => a.size <= b.size;
		public static bool operator >=(ByteSize a, ByteSize b) => a.size >= b.size;

		public override string ToString() => ShortString;
		public override int GetHashCode() => size.GetHashCode();
		public override bool Equals(object? other) => other is ByteSize size && Equals(size);
		public bool Equals(ByteSize other) => other.size.Equals(size);
		public int CompareTo(ByteSize other) => other.size.CompareTo(size);

		public ByteSize(ulong bytes)
		{
			if (bytes > long.MaxValue)
			{
				throw new ArgumentException($"The maximum size is {long.MaxValue}.");
			}
			size = ByteSizeLib.ByteSize.FromBytes((long)bytes);
		}
	}
}
