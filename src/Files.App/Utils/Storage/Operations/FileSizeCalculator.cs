// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.IO;
using Windows.Win32;
using Windows.Win32.Storage.FileSystem;

namespace Files.App.Utils.Storage.Operations
{
	internal sealed class FileSizeCalculator
	{
		private readonly string[] _paths;
		private readonly ConcurrentDictionary<string, long> _computedFiles = new();
		private long _size;

		public long Size => _size;
		public int ItemsCount => _computedFiles.Count;
		public bool Completed { get; private set; }

		public FileSizeCalculator(params string[] paths)
		{
			_paths = paths;
		}

		public async Task ComputeSizeAsync(CancellationToken cancellationToken = default)
		{
			await Parallel.ForEachAsync(
				_paths,
				cancellationToken,
				async (path, token) => await Task.Factory.StartNew(() =>
					{
						ComputeSizeRecursively(path, token);
					},
					token,
					TaskCreationOptions.LongRunning,
					TaskScheduler.Default));

			unsafe void ComputeSizeRecursively(string path, CancellationToken token)
			{
				var queue = new Queue<string>();
				if (!Win32Helper.HasFileAttribute(path, FileAttributes.Directory))
				{
					ComputeFileSize(path);
				}
				else
				{
					queue.Enqueue(path);

					while (queue.TryDequeue(out var directory))
					{
						WIN32_FIND_DATAW findData = default;

						fixed (char* pszFilePath = directory + "\\*.*")
						{
							var hFile = PInvoke.FindFirstFileEx(
								pszFilePath,
								FINDEX_INFO_LEVELS.FindExInfoBasic,
								&findData,
								FINDEX_SEARCH_OPS.FindExSearchNameMatch,
								null,
								FIND_FIRST_EX_FLAGS.FIND_FIRST_EX_LARGE_FETCH);

							if (!hFile.IsNull)
							{
								do
								{
									FILE_FLAGS_AND_ATTRIBUTES attributes = (FILE_FLAGS_AND_ATTRIBUTES)findData.dwFileAttributes;

									if (attributes.HasFlag(FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_REPARSE_POINT))
										// Skip symbolic links and junctions
										continue;

									var itemPath = Path.Combine(directory, findData.cFileName.ToString());

									if (attributes.HasFlag(FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_DIRECTORY))
									{
										ComputeFileSize(itemPath);
									}
									else if (findData.cFileName.ToString() is string fileName &&
										fileName.Equals(".", StringComparison.OrdinalIgnoreCase) &&
										fileName.Equals("..", StringComparison.OrdinalIgnoreCase))
									{
										queue.Enqueue(itemPath);
									}

									if (token.IsCancellationRequested)
										break;
								}
								while (PInvoke.FindNextFile(hFile, &findData));
							}

							PInvoke.CloseHandle(hFile);
						}
					}
				}
			}
		}

		private long ComputeFileSize(string path)
		{
			if (_computedFiles.TryGetValue(path, out var size))
				return size;

			using var hFile = PInvoke.CreateFile(
				path,
				(uint)FILE_ACCESS_RIGHTS.FILE_READ_ATTRIBUTES,
				FILE_SHARE_MODE.FILE_SHARE_READ,
				null,
				FILE_CREATION_DISPOSITION.OPEN_EXISTING,
				0,
				null);

			if (!hFile.IsInvalid && PInvoke.GetFileSizeEx(hFile, out size) && _computedFiles.TryAdd(path, size))
				Interlocked.Add(ref _size, size);

			return size;
		}

		public void ForceComputeFileSize(string path)
		{
			if (!Win32Helper.HasFileAttribute(path, FileAttributes.Directory))
				ComputeFileSize(path);
		}

		public bool TryGetComputedFileSize(string path, out long size)
		{
			return _computedFiles.TryGetValue(path, out size);
		}
	}
}
