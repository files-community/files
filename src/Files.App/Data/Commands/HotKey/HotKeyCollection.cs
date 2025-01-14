// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Immutable;

namespace Files.App.Data.Commands
{
	/// <summary>
	/// Represents immutable collection of <see cref="HotKey"/>.
	/// </summary>
	[DebuggerDisplay("{LocalizedLabel}")]
	public readonly struct HotKeyCollection : IEnumerable<HotKey>, IEquatable<HotKeyCollection>
	{
		// Fields

		private static readonly char[] parseSeparators = [' ', '\t'];

		private readonly ImmutableArray<HotKey> hotKeys;

		// Properties

		/// <summary>
		/// Gets empty instance of <see cref="HotKeyCollection"/>.
		/// </summary>
		public static HotKeyCollection Empty { get; } = new(ImmutableArray<HotKey>.Empty);

		public HotKey this[int index] => index >= 0 && index < hotKeys.Length ? hotKeys[index] : HotKey.None;

		/// <summary>
		/// Gets an value that indicates whether the hotkey collection is null.
		/// </summary>
		public bool IsEmpty
			=> hotKeys.IsDefaultOrEmpty;

		/// <inheritdoc cref="ImmutableArray{HotKey}.Length"/>
		public int Length
			=> hotKeys.Length;

		/// <summary>
		/// Gets the raw code of the hotkey, separated by a space.
		/// </summary>
		/// <remarks>
		/// For example, this is "Ctrl+A Ctrl+Menu+C"
		/// </remarks>
		public string RawLabel
			=> string.Join(' ', hotKeys.Select(hotKey => hotKey.RawLabel));

		/// <summary>
		/// Gets the humanized label of the hotkey to shown in the UI, separated by a command and double space.
		/// </summary>
		/// <remarks>
		/// For example, this is "Ctrl+A,  Ctrl+Alt+C"
		/// </remarks>
		public string LocalizedLabel
			=> string.Join(",  ", hotKeys.Where(hotKey => hotKey.IsVisible).Select(hotKey => hotKey.LocalizedLabel));

		// Constructors

		public HotKeyCollection()
		{
			hotKeys = [];
		}

		public HotKeyCollection(params HotKey[] hotKeys)
		{
			this.hotKeys = Standardize(hotKeys);
		}

		public HotKeyCollection(IEnumerable<HotKey> hotKeys)
		{
			this.hotKeys = Standardize(hotKeys);
		}

		// Operator overloads

		public static bool operator ==(HotKeyCollection hotKeysA, HotKeyCollection hotKeysB) => hotKeysA.Equals(hotKeysB);
		public static bool operator !=(HotKeyCollection hotKeysA, HotKeyCollection hotKeysB) => !hotKeysA.Equals(hotKeysB);

		/// <summary>
		/// Parses humanized hotkey code collection with separators.
		/// </summary>
		/// <param name="code">Humanized code to parse.</param>
		/// <param name="localized">Whether the code is localized.</param>
		/// <returns>Humanized code with a format <see cref="HotKeyCollection"/>.</returns>
		public static HotKeyCollection Parse(string code, bool localized = true)
		{
			var hotKeys = code
				.Split(parseSeparators)
				.Select(part => part.Trim())
				.Select(x => HotKey.Parse(x, localized));

			return new(hotKeys);
		}

		/// <inheritdoc cref="ImmutableArray{HotKey}.Contains"/>
		public bool Contains(HotKey hotKey)
			=> hotKeys.Contains(hotKey);

		/// <inheritdoc cref="IEnumerator{HotKey}.GetEnumerator"/>
		IEnumerator IEnumerable.GetEnumerator()
			=> GetEnumerator();

		/// <inheritdoc cref="IEnumerator{HotKey}.GetEnumerator"/>
		public IEnumerator<HotKey> GetEnumerator()
			=> (hotKeys as IEnumerable<HotKey>).GetEnumerator();

		// Default methods

		public override string ToString()
			=> LocalizedLabel;

		public override int GetHashCode()
			=> hotKeys.GetHashCode();

		public override bool Equals(object? other)
			=> other is HotKeyCollection hotKeys && Equals(hotKeys);

		public bool Equals(HotKeyCollection other)
			=> hotKeys.SequenceEqual(other.hotKeys);

		// Private methods

		private static ImmutableArray<HotKey> Standardize(IEnumerable<HotKey> hotKeys)
		{
			return hotKeys
				.Distinct()
				.Where(hotKey => !hotKey.IsNone)
				.GroupBy(hotKey => hotKey with { IsVisible = true })
				.Select(group => group.OrderBy(hotKey => hotKey.IsVisible).Last())
				.ToImmutableArray();
		}
	}
}