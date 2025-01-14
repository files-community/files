// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Windows.Input;

namespace Files.App.ViewModels.Properties
{
	/// <summary>
	/// Represents view model of <see cref="Views.Properties.CompatibilityPage"/>.
	/// </summary>
	public sealed class CompatibilityViewModel : ObservableObject
	{
		// Dependency injections

		private IWindowsCompatibilityService WindowsCompatibilityService { get; } = Ioc.Default.GetRequiredService<IWindowsCompatibilityService>();

		// Properties

		public string ItemPath { get; }

		public WindowsCompatibilityOptions CompatibilityOptions { get; }

		public Dictionary<WindowsCompatModeKind, string> CompatibilityModes { get; } = [];
		public Dictionary<WindowsCompatReducedColorModeKind, string> ReducedColorModes { get; } = [];
		public Dictionary<WindowsCompatDPIOptionKind, string> HighDpiOptions { get; } = [];
		public Dictionary<WindowsCompatDpiOverrideKind, string> HighDpiOverrides { get; } = [];

		public string SelectedCompatibilityMode
		{
			get => CompatibilityModes.GetValueOrDefault(CompatibilityOptions.CompatibilityMode)!;
			set => SetProperty(ref CompatibilityOptions.CompatibilityMode, CompatibilityModes.First(e => e.Value == value).Key);
		}

		public string SelectedReducedColorMode
		{
			get => ReducedColorModes.GetValueOrDefault(CompatibilityOptions.ReducedColorMode)!;
			set => SetProperty(ref CompatibilityOptions.ReducedColorMode, ReducedColorModes.First(e => e.Value == value).Key);
		}

		public bool RunIn40x480Resolution
		{
			get => CompatibilityOptions.RunIn40x480Resolution;
			set => SetProperty(ref CompatibilityOptions.RunIn40x480Resolution, value);
		}

		public bool DisableFullscreenOptimization
		{
			get => CompatibilityOptions.DisableFullscreenOptimization;
			set => SetProperty(ref CompatibilityOptions.DisableFullscreenOptimization, value);
		}

		public bool RunAsAdministrator
		{
			get => CompatibilityOptions.RunAsAdministrator;
			set => SetProperty(ref CompatibilityOptions.RunAsAdministrator, value);
		}

		public bool RegisterForRestart
		{
			get => CompatibilityOptions.RegisterForRestart;
			set => SetProperty(ref CompatibilityOptions.RegisterForRestart, value);
		}

		public string SelectedHighDpiOption
		{
			get => HighDpiOptions.GetValueOrDefault(CompatibilityOptions.HighDpiOption)!;
			set => SetProperty(ref CompatibilityOptions.HighDpiOption, HighDpiOptions.First(e => e.Value == value).Key);
		}

		public string SelectedHighDpiOverride
		{
			get => HighDpiOverrides.GetValueOrDefault(CompatibilityOptions.HighDpiOverride)!;
			set => SetProperty(ref CompatibilityOptions.HighDpiOverride, HighDpiOverrides.First(e => e.Value == value).Key);
		}

		// Commands

		public ICommand RunTroubleshooterCommand { get; set; }

		// Constructor

		public CompatibilityViewModel(ListedItem item)
		{
			ItemPath = item is IShortcutItem shortcutItem ? shortcutItem.TargetPath : item.ItemPath;

			CompatibilityOptions = WindowsCompatibilityService.GetCompatibilityOptionsForPath(ItemPath);

			CompatibilityModes.Add(WindowsCompatModeKind.None, "None".GetLocalizedResource());
			CompatibilityModes.Add(WindowsCompatModeKind.WindowsVista, "Windows Vista");
			CompatibilityModes.Add(WindowsCompatModeKind.WindowsVistaSP1, "Windows Vista (Service Pack 1)");
			CompatibilityModes.Add(WindowsCompatModeKind.WindowsVistaSP2, "Windows Vista (Service Pack 2)");
			CompatibilityModes.Add(WindowsCompatModeKind.Windows7, "Windows 7");
			CompatibilityModes.Add(WindowsCompatModeKind.Windows8, "Windows 8");

			ReducedColorModes.Add(WindowsCompatReducedColorModeKind.None, "CompatibilityNoReducedColor".GetLocalizedResource());
			ReducedColorModes.Add(WindowsCompatReducedColorModeKind.Color8Bit, "CompatibilityReducedColorModeColor8bit".GetLocalizedResource());
			ReducedColorModes.Add(WindowsCompatReducedColorModeKind.Color16Bit, "CompatibilityReducedColorModeColor16bit".GetLocalizedResource());

			HighDpiOptions.Add(WindowsCompatDPIOptionKind.None, "CompatibilityDoNotAdjustDPI".GetLocalizedResource());
			HighDpiOptions.Add(WindowsCompatDPIOptionKind.UseDPIOnLogin, "CompatibilityOnWindowsLogin".GetLocalizedResource());
			HighDpiOptions.Add(WindowsCompatDPIOptionKind.UseDPIOnProgramStart, "CompatibilityOnProgramStart".GetLocalizedResource());

			HighDpiOverrides.Add(WindowsCompatDpiOverrideKind.None, "CompatibilityDoNotOverrideDPI".GetLocalizedResource());
			HighDpiOverrides.Add(WindowsCompatDpiOverrideKind.Advanced, "Advanced".GetLocalizedResource());
			HighDpiOverrides.Add(WindowsCompatDpiOverrideKind.Application, "Application".GetLocalizedResource());
			HighDpiOverrides.Add(WindowsCompatDpiOverrideKind.System, "System".GetLocalizedResource());
			HighDpiOverrides.Add(WindowsCompatDpiOverrideKind.SystemAdvanced, "CompatibilitySystemEnhanced".GetLocalizedResource());

			RunTroubleshooterCommand = new AsyncRelayCommand(ExecuteRunTroubleshooterCommand);
		}

		// Methods

		public bool SetCompatibilityOptions()
		{
			return WindowsCompatibilityService.SetCompatibilityOptionsForPath(ItemPath, CompatibilityOptions);
		}

		private Task<bool> ExecuteRunTroubleshooterCommand()
		{
			return LaunchHelper.RunCompatibilityTroubleshooterAsync(ItemPath);
		}
	}
}
