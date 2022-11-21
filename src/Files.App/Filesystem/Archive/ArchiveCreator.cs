﻿using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Shared;
using SevenZip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Files.App.Filesystem.Archive
{
	public class ArchiveCreator : IArchiveCreator
	{
		private string archiveName = string.Empty;
		public string ArchiveName => archiveName;

		public string Directory { get; set; } = string.Empty;
		public string FileName { get; set; } = string.Empty;
		public string Password { get; set; } = string.Empty;

		public IEnumerable<string> Sources { get; set; } = Enumerable.Empty<string>();

		public ArchiveFormats FileFormat { get; set; } = ArchiveFormats.Zip;
		public bool DoNotCompress { get; set; } = false;
		public ArchiveSplittingSizes SplittingSize { get; set; } = ArchiveSplittingSizes.None;

		public IProgress<float> Progress { get; set; } = new Progress<float>();

		private string ArchiveExtension => FileFormat switch
		{
			ArchiveFormats.Zip => ".zip",
			ArchiveFormats.SevenZip => ".7z",
			_ => throw new ArgumentOutOfRangeException(nameof(FileFormat)),
		};
		private OutArchiveFormat SevenZipArchiveFormat => FileFormat switch
		{
			ArchiveFormats.Zip => OutArchiveFormat.Zip,
			ArchiveFormats.SevenZip => OutArchiveFormat.SevenZip,
			_ => throw new ArgumentOutOfRangeException(nameof(FileFormat)),
		};
		private long SevenZipVolumeSize => SplittingSize switch
		{
			ArchiveSplittingSizes.None => 0L,
			ArchiveSplittingSizes.Mo10 => 10 * 1000 * 1000L,
			ArchiveSplittingSizes.Mo100 => 100 * 1000 * 1000L,
			ArchiveSplittingSizes.Mo1024 => 1024 * 1000 * 1000L,
			ArchiveSplittingSizes.Mo5120 => 5120 * 1000 * 1000L,
			ArchiveSplittingSizes.Fat4092 => 4092 * 1000 * 1000L,
			ArchiveSplittingSizes.Cd650 => 650 * 1000 * 1000L,
			ArchiveSplittingSizes.Cd700 => 700 * 1000 * 1000L,
			ArchiveSplittingSizes.Dvd4480 => 4480 * 1000 * 1000L,
			ArchiveSplittingSizes.Dvd8128 => 8128 * 1000 * 1000L,
			ArchiveSplittingSizes.Bd23040 => 23040 * 1000 * 1000L,
			_ => throw new ArgumentOutOfRangeException(nameof(SplittingSize)),
		};

		public async Task<bool> RunCreationAsync()
		{
			var path = Path.Combine(Directory, FileName + ArchiveExtension);
			string[] sources = Sources.ToArray();

			int index = 1;
			while (File.Exists(path) || System.IO.Directory.Exists(path))
				path = Path.Combine(Directory, $"{FileName} ({++index}){ArchiveExtension}");
			archiveName = path;

			var compressor = new SevenZipCompressor
			{
				ArchiveFormat = SevenZipArchiveFormat,
				CompressionLevel = !DoNotCompress ? CompressionLevel.Ultra : CompressionLevel.None,
				VolumeSize = FileFormat is ArchiveFormats.SevenZip ? SevenZipVolumeSize : 0,
				FastCompression = false,
				IncludeEmptyDirectories = true,
				EncryptHeaders = true,
				PreserveDirectoryRoot = sources.Length > 1,
				EventSynchronization = EventSynchronizationStrategy.AlwaysAsynchronous,
			};

			compressor.Compressing += Compressor_Compressing;
			try
			{
				var files = sources.Where(source => File.Exists(source)).ToArray();
				var directories = sources.Where(source => System.IO.Directory.Exists(source)).ToArray();

				foreach (string directory in directories)
				{
					await compressor.CompressDirectoryAsync(directory, path, Password);
					compressor.CompressionMode = CompressionMode.Append;
				}

				if (string.IsNullOrEmpty(Password))
					await compressor.CompressFilesAsync(path, files);
				else
					await compressor.CompressFilesEncryptedAsync(path, Password, files);

				return true;
			}
			catch (Exception ex)
			{
				var logger = Ioc.Default.GetService<ILogger>();
				logger?.Warn(ex, $"Error compressing folder: {path}");

				return false;
			}
		}

		private void Compressor_Compressing(object? _, ProgressEventArgs e) => Progress.Report(e.PercentDone);
	}
}
