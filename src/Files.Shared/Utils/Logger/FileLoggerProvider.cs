// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Files.Shared
{
	public sealed class FileLoggerProvider : ILoggerProvider
	{
		private readonly string path;

		public FileLoggerProvider(string path)
		{
			this.path = path;
		}

		public ILogger CreateLogger(string categoryName)
		{
			var logger = new FileLogger(path);
			_ = Task.Run(() => logger.PurgeLogs(100));
			return logger;
		}

		public void Dispose()
		{
		}
	}
}
