// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Contracts
{
	/// <summary>
	/// Represents an interface for archive creation support.
	/// </summary>
	public interface ICompressArchiveModel
	{
		/// <summary>
		/// File path to archive.
		/// </summary>
		string ArchivePath { get; set; }

		/// <summary>
		/// Directory name.
		/// </summary>
		string Directory { get; }

		/// <summary>
		/// File name.
		/// </summary>
		string FileName { get; }

		/// <summary>
		/// Password.
		/// </summary>
		string Password { get; }

		/// <summary>
		/// Source.
		/// </summary>
		IEnumerable<string> Sources { get; }

		/// <summary>
		/// Archive file format.
		/// </summary>
		ArchiveFormats FileFormat { get; }

		/// <summary>
		/// Archive compression level.
		/// </summary>
		ArchiveCompressionLevels CompressionLevel { get; }

		/// <summary>
		/// 7zip archive splitting size.
		/// </summary>
		ArchiveSplittingSizes SplittingSize { get; }
		
		/// <summary>
		/// Number of CPU Threads to use.
		/// </summary>
		int CPUThreads { get; }

		/// <summary>
		/// Archiving progress.
		/// </summary>
		IProgress<StatusCenterItemProgressModel> Progress { get; set; }

		/// <summary>
		/// Cancellation request.
		/// </summary>
		CancellationToken CancellationToken { get; set; }

		/// <summary>
		/// Get path which target will be archived to.
		/// </summary>
		/// <param name="suffix"></param>
		/// <returns></returns>
		string GetArchivePath(string suffix = "");

		/// <summary>
		/// Run archive creation command.
		/// </summary>
		/// <returns></returns>
		Task<bool> RunCreationAsync();
	}
}
