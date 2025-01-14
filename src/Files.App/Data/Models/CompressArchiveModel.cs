// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Utils.Storage.Operations;
using Microsoft.Extensions.Logging;
using SevenZip;
using System.IO;

namespace Files.App.Data.Models
{
	/// <summary>
	/// Provides an archive creation support.
	/// </summary>
	public sealed class CompressArchiveModel : ICompressArchiveModel
	{
		private StatusCenterItemProgressModel _fileSystemProgress;

		private FileSizeCalculator _sizeCalculator;

		private IThreadingService _threadingService = Ioc.Default.GetRequiredService<IThreadingService>();

		private string ArchiveExtension => FileFormat switch
		{
			ArchiveFormats.Zip => ".zip",
			ArchiveFormats.SevenZip => ".7z",
			ArchiveFormats.Tar => ".tar",
			ArchiveFormats.GZip => ".gz",
			_ => throw new ArgumentOutOfRangeException(nameof(FileFormat)),
		};

		private OutArchiveFormat SevenZipArchiveFormat => FileFormat switch
		{
			ArchiveFormats.Zip => OutArchiveFormat.Zip,
			ArchiveFormats.SevenZip => OutArchiveFormat.SevenZip,
			ArchiveFormats.Tar => OutArchiveFormat.Tar,
			ArchiveFormats.GZip => OutArchiveFormat.GZip,
			_ => throw new ArgumentOutOfRangeException(nameof(FileFormat)),
		};

		private CompressionLevel SevenZipCompressionLevel => CompressionLevel switch
		{
			ArchiveCompressionLevels.Ultra => SevenZip.CompressionLevel.Ultra,
			ArchiveCompressionLevels.High => SevenZip.CompressionLevel.High,
			ArchiveCompressionLevels.Normal => SevenZip.CompressionLevel.Normal,
			ArchiveCompressionLevels.Low => SevenZip.CompressionLevel.Low,
			ArchiveCompressionLevels.Fast => SevenZip.CompressionLevel.Fast,
			ArchiveCompressionLevels.None => SevenZip.CompressionLevel.None,
			_ => throw new ArgumentOutOfRangeException(nameof(CompressionLevel)),
		};

		private long SevenZipVolumeSize => SplittingSize switch
		{
			ArchiveSplittingSizes.None => 0L,
			ArchiveSplittingSizes.Mo10 => 10 * 1000 * 1000L,
			ArchiveSplittingSizes.Mo100 => 100 * 1000 * 1000L,
			ArchiveSplittingSizes.Mo1024 => 1024 * 1000 * 1000L,
			ArchiveSplittingSizes.Mo2048 => 2048 * 1000 * 1000L,
			ArchiveSplittingSizes.Mo5120 => 5120 * 1000 * 1000L,
			ArchiveSplittingSizes.Fat4092 => 4092 * 1000 * 1000L,
			ArchiveSplittingSizes.Cd650 => 650 * 1000 * 1000L,
			ArchiveSplittingSizes.Cd700 => 700 * 1000 * 1000L,
			ArchiveSplittingSizes.Dvd4480 => 4480 * 1000 * 1000L,
			ArchiveSplittingSizes.Dvd8128 => 8128 * 1000 * 1000L,
			ArchiveSplittingSizes.Bd23040 => 23040 * 1000 * 1000L,
			_ => throw new ArgumentOutOfRangeException(nameof(SplittingSize)),
		};

		private IProgress<StatusCenterItemProgressModel> _Progress;
		public IProgress<StatusCenterItemProgressModel> Progress
		{
			get => _Progress;
			set
			{
				_Progress = value;

				_fileSystemProgress = new(
					Progress,
					false,
					FileSystemStatusCode.InProgress);

				_fileSystemProgress.Report(0);
			}
		}

		/// <inheritdoc/>
		public string ArchivePath { get; set; }

		/// <inheritdoc/>
		public string Directory { get; init; }

		/// <inheritdoc/>
		public string FileName { get; init; }

		/// <inheritdoc/>
		public string Password { get; init; }

		/// <inheritdoc/>
		public IEnumerable<string> Sources { get; init; }

		/// <inheritdoc/>
		public ArchiveFormats FileFormat { get; init; }

		/// <inheritdoc/>
		public ArchiveCompressionLevels CompressionLevel { get; init; }

		/// <inheritdoc/>
		public ArchiveSplittingSizes SplittingSize { get; init; }

		/// <inheritdoc/>
		public CancellationToken CancellationToken { get; set; }
		
		/// <inheritdoc/>
		public int CPUThreads { get; set; }

		public CompressArchiveModel(
			string[] source,
			string directory,
			string fileName,
			int cpuThreads,
			string? password = null,
			ArchiveFormats fileFormat = ArchiveFormats.Zip,
			ArchiveCompressionLevels compressionLevel = ArchiveCompressionLevels.Normal,
			ArchiveSplittingSizes splittingSize = ArchiveSplittingSizes.None)
		{
			_Progress = new Progress<StatusCenterItemProgressModel>();

			Sources = source;
			Directory = directory;
			FileName = fileName;
			Password = password ?? string.Empty;
			ArchivePath = string.Empty;
			FileFormat = fileFormat;
			CompressionLevel = compressionLevel;
			SplittingSize = splittingSize;
			CPUThreads = cpuThreads;
		}

		/// <inheritdoc/>
		public string GetArchivePath(string suffix = "")
		{
			return Path.Combine(Directory, $"{FileName}{suffix}{ArchiveExtension}");
		}

		/// <inheritdoc/>
		public async Task<bool> RunCreationAsync()
		{
			string[] sources = Sources.ToArray();

			var compressor = new SevenZipCompressor()
			{
				ArchiveFormat = SevenZipArchiveFormat,
				CompressionLevel = SevenZipCompressionLevel,
				VolumeSize = FileFormat is ArchiveFormats.SevenZip ? SevenZipVolumeSize : 0,
				FastCompression = false,
				IncludeEmptyDirectories = true,
				EncryptHeaders = true,
				PreserveDirectoryRoot = sources.Length > 1,
			};

			compressor.CustomParameters.Add("mt", CPUThreads.ToString());

			compressor.Compressing += Compressor_Compressing;
			compressor.FileCompressionStarted += Compressor_FileCompressionStarted;
			compressor.FileCompressionFinished += Compressor_FileCompressionFinished;

			var cts = new CancellationTokenSource();

			try
			{
				var files = sources.Where(File.Exists).ToArray();
				var directories = sources.Where(SystemIO.Directory.Exists);

				_sizeCalculator = new FileSizeCalculator([.. files, .. directories]);
				var sizeTask = _sizeCalculator.ComputeSizeAsync(cts.Token);
				_ = sizeTask.ContinueWith(_ =>
				{
					_fileSystemProgress.TotalSize = _sizeCalculator.Size;
					_fileSystemProgress.ItemsCount = _sizeCalculator.ItemsCount;
					_fileSystemProgress.EnumerationCompleted = true;
					_fileSystemProgress.Report();
				});

				foreach (string directory in directories)
				{
					try
					{
						await compressor.CompressDirectoryAsync(directory, ArchivePath, Password);
					}
					catch (SevenZipInvalidFileNamesException)
					{
						// The directory has no files, so we need to create entries manually
						var fileDictionary = new Dictionary<string, string>();
						AddEntry(fileDictionary, directory, "");

						compressor.CompressFileDictionary(fileDictionary, ArchivePath, Password);

						static void AddEntry(IDictionary<string, string> fileDictionary, string directory, string entryPrefix)
						{
							DirectoryInfo directoryInfo = new DirectoryInfo(directory);

							DirectoryInfo[] directories = directoryInfo.GetDirectories();
							if (directories.Length == 0)
							{
								fileDictionary.Add(entryPrefix + directoryInfo.Name, null);
							}
							else
							{
								entryPrefix += directoryInfo.Name + Path.DirectorySeparatorChar;
								foreach (DirectoryInfo directoryInfo2 in directories)
									AddEntry(fileDictionary, directoryInfo2.FullName, entryPrefix);
							}
						}
					}

					compressor.CompressionMode = CompressionMode.Append;
				}

				if (files.Any())
				{
					if (string.IsNullOrEmpty(Password))
						await compressor.CompressFilesAsync(ArchivePath, files);
					else
						await compressor.CompressFilesEncryptedAsync(ArchivePath, Password, files);
				}

				cts.Cancel();

				return true;
			}
			catch (Exception ex)
			{
				var logger = Ioc.Default.GetRequiredService<ILogger<App>>();
				logger?.LogWarning(ex, $"Error compressing folder: {ArchivePath}");

				cts.Cancel();

				return false;
			}
		}

		private void Compressor_FileCompressionStarted(object? sender, FileNameEventArgs e)
		{
			if (CancellationToken.IsCancellationRequested)
				e.Cancel = true;
			else
				_sizeCalculator.ForceComputeFileSize(e.FilePath);
			_threadingService.ExecuteOnUiThreadAsync(() =>
			{
				_fileSystemProgress.FileName = e.FileName;
				_fileSystemProgress.Report();
			});
		}

		private void Compressor_FileCompressionFinished(object? sender, EventArgs e)
		{
			_fileSystemProgress.AddProcessedItemsCount(1);
			_fileSystemProgress.Report();
		}

		private void Compressor_Compressing(object? _, ProgressEventArgs e)
		{
			if (_fileSystemProgress.TotalSize > 0)
				_fileSystemProgress.Report((_fileSystemProgress.ProcessedSize + e.PercentDelta / 100.0 * e.BytesCount) / _fileSystemProgress.TotalSize * 100);
		}
	}
}
