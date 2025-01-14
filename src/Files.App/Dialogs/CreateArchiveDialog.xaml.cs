// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Immutable;
using Windows.Foundation.Metadata;

namespace Files.App.Dialogs
{
	public sealed partial class CreateArchiveDialog : ContentDialog
	{
		private FrameworkElement RootAppElement
			=> (FrameworkElement)MainWindow.Instance.Content;

		private bool canCreate = false;
		public bool CanCreate => canCreate;

		public string FileName
		{
			get => ViewModel.FileName;
			set => ViewModel.FileName = value;
		}

		public bool UseEncryption
		{
			get => ViewModel.UseEncryption;
			set => ViewModel.UseEncryption = value;
		}

		public string Password
		{
			get => ViewModel.Password;
			set => ViewModel.Password = value;
		}

		public ArchiveFormats FileFormat
		{
			get => ViewModel.FileFormat.Key;
			set => ViewModel.FileFormat = ViewModel.FileFormats.First(format => format.Key == value);
		}

		public ArchiveCompressionLevels CompressionLevel
		{
			get => ViewModel.CompressionLevel.Key;
			set => ViewModel.CompressionLevel = ViewModel.CompressionLevels.First(level => level.Key == value);
		}

		public ArchiveSplittingSizes SplittingSize
		{
			get => ViewModel.SplittingSize.Key;
			set => ViewModel.SplittingSize = ViewModel.SplittingSizes.First(size => size.Key == value);
		}

		public int CPUThreads
		{
			get => ViewModel.CPUThreads;
			set => ViewModel.CPUThreads = value;
		}

		private DialogViewModel ViewModel { get; } = new();

		public CreateArchiveDialog()
		{
			InitializeComponent();

			ViewModel.PropertyChanged += ViewModel_PropertyChanged;
		}

		public new Task<ContentDialogResult> ShowAsync()
		{
			return SetContentDialogRoot(this).ShowAsync().AsTask();
		}

		private static ContentDialog SetContentDialogRoot(ContentDialog contentDialog)
		{
			if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				contentDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot; // WinUi3
			return contentDialog;
		}

		private void ContentDialog_Loaded(object _, RoutedEventArgs e)
		{
			Loaded -= ContentDialog_Loaded;

			FileNameBox.SelectionStart = FileNameBox.Text.Length;
			FileNameBox.Focus(FocusState.Programmatic);
		}
		private void ContentDialog_Closing(ContentDialog _, ContentDialogClosingEventArgs e)
		{
			InvalidNameWarning.IsOpen = false;
			Closing -= ContentDialog_Closing;
			ViewModel.PropertyChanged -= ViewModel_PropertyChanged;

			if (e.Result is ContentDialogResult.Primary)
				canCreate = true;
		}

		private void PasswordBox_Loading(FrameworkElement _, object e)
			=> PasswordBox.Focus(FocusState.Programmatic);

		private void ViewModel_PropertyChanged(object? _, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(DialogViewModel.UseEncryption) && ViewModel.UseEncryption)
				PasswordBox.Focus(FocusState.Programmatic);
		}

		private sealed class DialogViewModel : ObservableObject
		{
			private readonly IGeneralSettingsService GeneralSettingsService = Ioc.Default.GetRequiredService<IGeneralSettingsService>();

			public bool IsNameValid => FilesystemHelpers.IsValidForFilename(fileName);

			public bool ShowNameWarning => !string.IsNullOrEmpty(fileName) && !IsNameValid;

			private string fileName = string.Empty;
			public string FileName
			{
				get => fileName;
				set
				{
					if (SetProperty(ref fileName, value))
					{
						OnPropertyChanged(nameof(IsNameValid));
						OnPropertyChanged(nameof(ShowNameWarning));
					}
				}
			}

			public FileFormatItem FileFormat
			{
				get => FileFormats.First(format => format.Key == GeneralSettingsService.ArchiveFormatsOption);
				set
				{
					if (value.Key != GeneralSettingsService.ArchiveFormatsOption)
					{
						GeneralSettingsService.ArchiveFormatsOption = value.Key;
						OnPropertyChanged(nameof(CanSplit));
					}
				}
			}

			public CompressionLevelItem CompressionLevel
			{
				get => CompressionLevels.First(level => level.Key == GeneralSettingsService.ArchiveCompressionLevelsOption);
				set
				{
					if (value.Key != GeneralSettingsService.ArchiveCompressionLevelsOption)
						GeneralSettingsService.ArchiveCompressionLevelsOption = value.Key;
				}
			}

			public bool CanSplit => FileFormat.Key is ArchiveFormats.SevenZip;

			public SplittingSizeItem SplittingSize
			{
				get => SplittingSizes.First(size => size.Key == GeneralSettingsService.ArchiveSplittingSizesOption);
				set
				{
					if (value.Key != GeneralSettingsService.ArchiveSplittingSizesOption)
						GeneralSettingsService.ArchiveSplittingSizesOption = value.Key;
				}
			}

			private int cpuThreads = Environment.ProcessorCount;
			public int CPUThreads
			{
				get => cpuThreads;
				set => SetProperty(ref cpuThreads, value);
			}

			private bool useEncryption = false;
			public bool UseEncryption
			{
				get => useEncryption;
				set
				{
					if (SetProperty(ref useEncryption, value) && !useEncryption)
						Password = string.Empty;
				}
			}

			private string password = string.Empty;
			public string Password
			{
				get => password;
				set
				{
					if (SetProperty(ref password, value) && !string.IsNullOrEmpty(password))
						UseEncryption = true;
				}
			}

			public ImmutableList<FileFormatItem> FileFormats { get; } =
			[
				new(ArchiveFormats.Zip, ".zip"),
				new(ArchiveFormats.SevenZip, ".7z"),
			];

			public ImmutableList<CompressionLevelItem> CompressionLevels { get; } =
			[
				new CompressionLevelItem(ArchiveCompressionLevels.Ultra, "CompressionLevelUltra".GetLocalizedResource()),
				new CompressionLevelItem(ArchiveCompressionLevels.High, "CompressionLevelHigh".GetLocalizedResource()),
				new CompressionLevelItem(ArchiveCompressionLevels.Normal, "CompressionLevelNormal".GetLocalizedResource()),
				new CompressionLevelItem(ArchiveCompressionLevels.Low, "CompressionLevelLow".GetLocalizedResource()),
				new CompressionLevelItem(ArchiveCompressionLevels.Fast, "CompressionLevelFast".GetLocalizedResource()),
				new CompressionLevelItem(ArchiveCompressionLevels.None, "CompressionLevelNone".GetLocalizedResource()),
			];

			public ImmutableList<SplittingSizeItem> SplittingSizes { get; } =
			[
				new(ArchiveSplittingSizes.None, "Do not split".GetLocalizedResource()),
				new(ArchiveSplittingSizes.Mo10, ToSizeText(10)),
				new(ArchiveSplittingSizes.Mo100, ToSizeText(100)),
				new(ArchiveSplittingSizes.Cd650, ToSizeText(650), "CD".GetLocalizedResource()),
				new(ArchiveSplittingSizes.Cd700, ToSizeText(700), "CD".GetLocalizedResource()),
				new(ArchiveSplittingSizes.Mo1024, ToSizeText(1024)),
				new(ArchiveSplittingSizes.Mo2048, ToSizeText(2048)),
				new(ArchiveSplittingSizes.Fat4092, ToSizeText(4092), "FAT".GetLocalizedResource()),
				new(ArchiveSplittingSizes.Dvd4480, ToSizeText(4480), "DVD".GetLocalizedResource()),
				new(ArchiveSplittingSizes.Mo5120, ToSizeText(5120)),
				new(ArchiveSplittingSizes.Dvd8128, ToSizeText(8128), "DVD".GetLocalizedResource()),
				new(ArchiveSplittingSizes.Bd23040, ToSizeText(23040), "Bluray".GetLocalizedResource()),
			];

			public DialogViewModel()
			{

			}

			private static string ToSizeText(ulong megaBytes) => ByteSize.FromMebiBytes(megaBytes).ShortString;

			public record FileFormatItem(ArchiveFormats Key, string Label);

			public record CompressionLevelItem(ArchiveCompressionLevels Key, string Label);
		}
	}

	internal record SplittingSizeItem(ArchiveSplittingSizes Key, string Label, string Description = "")
	{
		public string Separator => string.IsNullOrEmpty(Description) ? string.Empty : "-";
	}
}
