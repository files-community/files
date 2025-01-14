// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;

namespace Files.App.Utils.CommandLine
{
	/// <summary>
	/// Provides static helper for parsing of command line arguments on Windows.
	/// </summary>
	public sealed class CommandLineParser
	{
		/// <summary>
		/// Parses raw command line string.
		/// </summary>
		/// <param name="cmdLineString">String of command line to parse.</param>
		/// <returns>A collection of parsed command.</returns>
		public static ParsedCommands ParseUntrustedCommands(string cmdLineString)
		{
			var parsedArgs = Parse(SplitArguments(cmdLineString, true));

			return ParseSplitArguments(parsedArgs);
		}

		/// <summary>
		/// Parses raw command line string.
		/// </summary>
		/// <param name="cmdLineStrings">A string array of command line to parse.</param>
		/// <returns>A collection of parsed command.</returns>
		public static ParsedCommands ParseUntrustedCommands(string[] cmdLineStrings)
		{
			var parsedArgs = Parse(cmdLineStrings);

			return ParseSplitArguments(parsedArgs);
		}

		/// <summary>
		/// Convert string split arguments into a collection of <see cref="ParsedCommand"/>
		/// </summary>
		/// <param name="parsedArgs">A string array of command line to parse.</param>
		/// <returns>A collection of parsed command.</returns>
		private static ParsedCommands ParseSplitArguments(List<KeyValuePair<string, string[]>> parsedArgs)
		{
			var commands = new ParsedCommands();

			foreach (var kvp in parsedArgs)
			{
				Debug.WriteLine("arg {0} = {1}", kvp.Key, kvp.Value);

				var command = new ParsedCommand();

				switch (kvp.Key)
				{
					case string s when "Directory".Equals(s, StringComparison.OrdinalIgnoreCase):
						command.Type = ParsedCommandType.OpenDirectory;
						break;

					case string s when "OutputPath".Equals(s, StringComparison.OrdinalIgnoreCase):
						command.Type = ParsedCommandType.OutputPath;
						break;

					case string s when "Select".Equals(s, StringComparison.OrdinalIgnoreCase):
						command.Type = ParsedCommandType.SelectItem;
						break;

					case string s when "Tag".Equals(s, StringComparison.OrdinalIgnoreCase):
						command.Type = ParsedCommandType.TagFiles;
						break;

					default: //case "Cmdless":
						try
						{
							if (kvp.Value[0].StartsWith("::{", StringComparison.Ordinal) || kvp.Value[0].StartsWith("shell:", StringComparison.OrdinalIgnoreCase))
							{
								command.Type = ParsedCommandType.ExplorerShellCommand;
							}
							else if (Path.IsPathRooted(kvp.Value[0]))
							{
								command.Type = ParsedCommandType.OpenPath;
							}
							else
							{
								command.Type = ParsedCommandType.Unknown;
							}
						}
						catch (Exception ex)
						{
							Debug.WriteLine($"Exception in CommandLineParser.cs\\ParseUntrustedCommands with message: {ex.Message}");
							command.Type = ParsedCommandType.Unknown;
						}

						break;
				}

				command.Args.AddRange(kvp.Value);
				commands.Add(command);
			}

			return commands;
		}

		/// <summary>
		/// Split flat string argument to an array of <see cref="string"/>.
		/// </summary>
		/// <param name="commandLine"></param>
		/// <param name="trimQuotes"></param>
		/// <returns></returns>
		public static string[] SplitArguments(string commandLine, bool trimQuotes = false)
		{
			char[] commandLineCharArray = commandLine.ToCharArray();
			bool isInQuote = false;

			for (int i = 0; i < commandLineCharArray.Length; i++)
			{
				if (commandLineCharArray[i] == '"')
					isInQuote = !isInQuote;

				if (!isInQuote && commandLineCharArray[i] == ' ')
					commandLineCharArray[i] = '\n';
			}

			return trimQuotes
				? new string(commandLineCharArray).Replace("\"", "", StringComparison.Ordinal).Split('\n')
				: new string(commandLineCharArray).Split('\n');
		}

		/// <summary>
		/// Parse an arguments array of <see cref="string"/> to a collection of <see cref="KeyValuePair"/>.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		public static List<KeyValuePair<string, string[]>> Parse(string[] args = null)
		{
			var parsedArgs = new List<KeyValuePair<string, string[]>>();

			// This is better but I haven't tested this enough.
			//Environment.GetCommandLineArgs()

			if (args is not null)
			{
				//if - or / are not used then add the command as-is

				if (args.Length > 2)
				{
					for (int i = 0; i < args.Length; i++)
					{
						if (args[i].StartsWith('-') || args[i].StartsWith('/'))
						{
							var data = ParseData(args, i);

							if (data.Key is not null)
							{
								for (int j = 0; j < parsedArgs.Count; j++)
								{
									if (parsedArgs[j].Key == data.Key)
										parsedArgs.RemoveAt(j);
								}

								parsedArgs.Add(data);
							}
						}
					}
				}
			}

			if (parsedArgs.Count == 0 && args.Length >= 2)
				parsedArgs.Add(new KeyValuePair<string, string[]>("Cmdless", [string.Join(' ', args.Skip(1)).TrimStart()]));

			return parsedArgs;
		}

		/// <summary>
		/// Parse an arguments array of <see cref="string"/> to a collection of <see cref="KeyValuePair"/>.
		/// </summary>
		/// <param name="args"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		private static KeyValuePair<string, string[]> ParseData(string[] args, int index)
		{
			string? key = null;
			var val = new List<string>();

			if (args[index].StartsWith('-') || args[index].StartsWith('/'))
			{
				if (args[index].Contains(':', StringComparison.Ordinal))
				{
					string argument = args[index];
					int endIndex = argument.IndexOf(':');

					// Trim the '/' and the ':'
					key = argument.Substring(1, endIndex - 1);

					int valueStart = endIndex + 1;

					if (valueStart < argument.Length)
					{
						val.Add(argument[valueStart..]);
					}
				}
				else
				{
					key = args[index].Substring(1);
				}

				int argIndex = 1 + index;

				while (argIndex < args.Length && !(args[argIndex].StartsWith('-') || args[argIndex].StartsWith('/')))
				{
					val.Add(args[argIndex++]);
				}
			}

			return
				key is not null
					? new KeyValuePair<string, string[]>(key, [.. val])
					: default;
		}
	}
}
