// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Commands
{
	public interface IModifiableCommandManager : IEnumerable<IRichCommand>
	{
		IRichCommand this[CommandCodes code] { get; }

		IRichCommand None { get; }

		IRichCommand PasteItem { get; }
		IRichCommand DeleteItem { get; }
		IRichCommand OpenProperties { get; }
	}
}
