// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Items
{
	public sealed class WindowsCompatibilityOptions
	{
		// Constants

		private const string RegExecuteAt640X480 = "640X480";
		private const string RegDisableMaximized = "DISABLEDXMAXIMIZEDWINDOWEDMODE";
		private const string RegRunAsAdministrator = "RUNASADMIN";
		private const string RegRegisterForRestart = "REGISTERAPPRESTART";

		//Fields

		public WindowsCompatModeKind CompatibilityMode;
		public WindowsCompatReducedColorModeKind ReducedColorMode;
		public bool RunIn40x480Resolution;
		public bool DisableFullscreenOptimization;
		public bool RunAsAdministrator;
		public bool RegisterForRestart;
		public WindowsCompatDPIOptionKind HighDpiOption;
		public WindowsCompatDpiOverrideKind HighDpiOverride;

		// Methods

		public static WindowsCompatibilityOptions FromString(string? rawValue)
		{
			var compatOptions = new WindowsCompatibilityOptions();
			if (!string.IsNullOrEmpty(rawValue))
			{
				var components = rawValue.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (var value in components)
				{
					compatOptions.HighDpiOption |= ComponentModelExtensions.GetValueFromDescription<WindowsCompatDPIOptionKind>(value);
					compatOptions.HighDpiOverride |= ComponentModelExtensions.GetValueFromDescription<WindowsCompatDpiOverrideKind>(value);
					compatOptions.ReducedColorMode |= ComponentModelExtensions.GetValueFromDescription<WindowsCompatReducedColorModeKind>(value);
					compatOptions.CompatibilityMode |= ComponentModelExtensions.GetValueFromDescription<WindowsCompatModeKind>(value);
					compatOptions.RunIn40x480Resolution |= value == RegExecuteAt640X480;
					compatOptions.DisableFullscreenOptimization |= value == RegDisableMaximized;
					compatOptions.RunAsAdministrator |= value == RegRunAsAdministrator;
					compatOptions.RegisterForRestart |= value == RegRegisterForRestart;
				}
			}

			return compatOptions;
		}

		public override string ToString()
		{
			var value =
				$"~ {CompatibilityMode.GetDescription()} {ReducedColorMode.GetDescription()} {HighDpiOption.GetDescription()} {HighDpiOverride.GetDescription()} " +
				$"{(RunIn40x480Resolution ? RegExecuteAt640X480 : "")} {(DisableFullscreenOptimization ? RegDisableMaximized : "")} " +
				$"{(RunAsAdministrator ? RegRunAsAdministrator : "")} {(RegisterForRestart ? RegRegisterForRestart : "")}";

			return RegexHelpers.WhitespaceAtLeastOnce().Replace(value.Trim(), " ");
		}
	}
}
