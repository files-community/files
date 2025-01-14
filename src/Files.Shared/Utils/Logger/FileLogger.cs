// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Files.Shared
{
	public sealed class FileLogger : ILogger
	{
		private readonly object syncRoot = new();
		private readonly string filePath;

		public FileLogger(string filePath)
		{
			this.filePath = filePath;
		}

		public IDisposable? BeginScope<TState>(TState state) where TState : notnull
		{
			return null;
		}

		public bool IsEnabled(LogLevel logLevel)
		{
			return true;
		}

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			if (formatter is null)
				return;

			try
			{
				var message = exception?.ToString() ?? formatter(state, exception);

				lock (syncRoot)
				{
					File.AppendAllText(filePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.ffff}|{logLevel}|{message}" + Environment.NewLine);
				}
			}
			catch (Exception e)
			{
				Debug.WriteLine($"Writing to log file failed with the following exception:\n{e}");
			}
		}

		public void PurgeLogs(int numberOfLinesKept)
		{
			if (!File.Exists(filePath))
				return;

			try
			{
				lock (syncRoot)
				{
					var lines = File.ReadAllLines(filePath);
					if (lines.Length > numberOfLinesKept)
					{
						var lastLines = lines.Skip(Math.Max(0, lines.Length - numberOfLinesKept));
						File.WriteAllLines(filePath, lastLines);
					}
				}
			}
			catch (Exception e)
			{
				Debug.WriteLine($"Purging the log file failed with the following exception:\n{e}");
			}
		}
	}
}
