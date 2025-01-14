// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Sentry;
using Sentry.Protocol;

namespace Files.App.Utils.Logger
{
	public sealed class SentryLogger : ILogger
	{

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
			// Unhandled exceptions are captured in AppLifecycleHelper.HandleAppUnhandledException
			if (exception is null ||
				exception.Data[Mechanism.HandledKey] is false ||
				logLevel <= LogLevel.Information)
				return;

			var generalSettingsService = Ioc.Default.GetRequiredService<IGeneralSettingsService>();

			var level = logLevel switch
			{
				LogLevel.Debug => SentryLevel.Debug,
				LogLevel.Information => SentryLevel.Info,
				LogLevel.Warning => SentryLevel.Warning,
				LogLevel.Error => SentryLevel.Error,
				LogLevel.Critical => SentryLevel.Fatal,
				_ => SentryLevel.Debug
			};

			SentrySdk.CaptureException(exception, scope =>
			{
				scope.User.Id = generalSettingsService?.UserId;
				scope.Level = level;
			});
		}
	}
}
