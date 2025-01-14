// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Frozen;

namespace Files.App.Data.Commands
{
	internal sealed class ModifiableCommandManager : IModifiableCommandManager
	{
		private static readonly ICommandManager Commands = Ioc.Default.GetRequiredService<ICommandManager>();

		private readonly FrozenDictionary<CommandCodes, IRichCommand> ModifiableCommands;

		public IRichCommand this[CommandCodes code] => ModifiableCommands.TryGetValue(code, out var command) ? command : None;

		public IRichCommand None => ModifiableCommands[CommandCodes.None];
		public IRichCommand PasteItem => ModifiableCommands[CommandCodes.PasteItem];
		public IRichCommand DeleteItem => ModifiableCommands[CommandCodes.DeleteItem];
		public IRichCommand OpenProperties => ModifiableCommands[CommandCodes.OpenProperties];

		public ModifiableCommandManager()
		{
			ModifiableCommands = CreateModifiableCommands();
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		public IEnumerator<IRichCommand> GetEnumerator() => (ModifiableCommands.Values as IEnumerable<IRichCommand>).GetEnumerator();

		private static FrozenDictionary<CommandCodes, IRichCommand> CreateModifiableCommands() => new Dictionary<CommandCodes, IRichCommand>
		{
			[CommandCodes.None] = new NoneCommand(),
			[CommandCodes.PasteItem] = new ModifiableCommand(Commands.PasteItem, new() {
				{ KeyModifiers.Shift,  Commands.PasteItemToSelection }
			}),
			[CommandCodes.DeleteItem] = new ModifiableCommand(Commands.DeleteItem, new() {
				{ KeyModifiers.Shift,  Commands.DeleteItemPermanently }
			}),
			[CommandCodes.OpenProperties] = new ModifiableCommand(Commands.OpenProperties, new() {
				{ KeyModifiers.Shift,  Commands.OpenClassicProperties }
			}),
		}.ToFrozenDictionary();
	}
}
