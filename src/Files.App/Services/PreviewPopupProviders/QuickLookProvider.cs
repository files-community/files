// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text;

namespace Files.App.Services.PreviewPopupProviders
{
	public sealed class QuickLookProvider : IPreviewPopupProvider
	{
		public static QuickLookProvider Instance { get; } = new();

		private const int TIMEOUT = 500;
		private static string pipeName = $"QuickLook.App.Pipe.{WindowsIdentity.GetCurrent().User?.Value}";
		private static string pipeMessageSwitch = "QuickLook.App.PipeMessages.Switch";
		private static string pipeMessageToggle = "QuickLook.App.PipeMessages.Toggle";

		public async Task TogglePreviewPopupAsync(string path)
		{
			await DoPreviewAsync(path, pipeMessageToggle);
		}

		public async Task SwitchPreviewAsync(string path)
		{
			await DoPreviewAsync(path, pipeMessageSwitch);
		}

		private async Task DoPreviewAsync(string path, string message)
		{
			string pipeName = $"QuickLook.App.Pipe.{WindowsIdentity.GetCurrent().User?.Value}";

			await using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);
			try
			{
				await client.ConnectAsync(TIMEOUT);

				await using var writer = new StreamWriter(client);
				await writer.WriteLineAsync($"{message}|{path}");
				await writer.FlushAsync();
			}
			catch (Exception ex) when (ex is TimeoutException or IOException or EncoderFallbackException)
			{
				// ignore
			}
		}

		public async Task<bool> DetectAvailability()
		{
			async Task<int> QuickLookServerAvailable()
			{
				await using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);
				try
				{
					await client.ConnectAsync(TIMEOUT);
					var serverInstances = client.NumberOfServerInstances;

					await using var writer = new StreamWriter(client);
					await writer.WriteLineAsync($"{pipeMessageSwitch}|");
					await writer.FlushAsync();

					return serverInstances;
				}
				catch (Exception ex) when (ex is TimeoutException or IOException)
				{
					return 0;
				}
			}

			try
			{
				var result = await QuickLookServerAvailable();
				return result != 0;
			}
			catch (Exception ex)
			{
				App.Logger.LogInformation(ex, ex.Message);
				return false;
			}
		}
	}
}
