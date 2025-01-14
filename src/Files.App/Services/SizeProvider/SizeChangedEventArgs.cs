// Copyright (c) Files Community
// Licensed under the MIT License.

using System;

namespace Files.App.Services.SizeProvider
{
	public sealed class SizeChangedEventArgs : EventArgs
	{
		public string Path { get; }
		public ulong NewSize { get; }
		public SizeChangedValueState ValueState { get; }

		public SizeChangedEventArgs(string path, ulong newSize = 0, SizeChangedValueState valueState = SizeChangedValueState.None)
			=> (Path, NewSize, ValueState) = (path, newSize, valueState);
	}
}
