// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Backend.Services;
using Microsoft.Extensions.Logging;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using Vanara.PInvoke;

namespace Files.App.ServicesImplementation.PreviewPopupProviders
{
	public class SeerProProvider : IPreviewPopupProvider
	{
		public static SeerProProvider Instance { get; } = new();

		private const int TIMEOUT = 500;

		public async Task TogglePreviewPopup(string path)
		{
			bool isSeerProAvailable = await DetectAvailability();
			if (!isSeerProAvailable)
				return;
		}

		public async Task SwitchPreview(string path)
		{
			bool isSeerProAvailable = await DetectAvailability();
			if (!isSeerProAvailable)
				return;
		}

		private async Task DoPreview()
		{

		}

		public async Task<bool> DetectAvailability()
		{
			var handle = User32.FindWindow("SeerWindowClass", null).DangerousGetHandle();
			return handle != IntPtr.Zero && handle.ToInt64() != -1;
		}
	}
}
