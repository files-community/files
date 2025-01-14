// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Extensions;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static Files.App.Helpers.Win32Helper;

namespace Files.App.Services.SizeProvider
{
	public sealed class CachedSizeProvider : ISizeProvider
	{
		private readonly ConcurrentDictionary<string, ulong> sizes = new();

		public event EventHandler<SizeChangedEventArgs>? SizeChanged;

		public Task CleanAsync() => Task.CompletedTask;

		public Task ClearAsync()
		{
			sizes.Clear();
			return Task.CompletedTask;
		}

		public async Task UpdateAsync(string path, CancellationToken cancellationToken)
		{
			await Task.Yield();
			if (sizes.TryGetValue(path, out ulong cachedSize))
			{
				RaiseSizeChanged(path, cachedSize, SizeChangedValueState.Final);
			}
			else
			{
				RaiseSizeChanged(path, 0, SizeChangedValueState.None);
			}

			var stopwatch = Stopwatch.StartNew();
			ulong size = await Calculate(path);

			sizes[path] = size;
			RaiseSizeChanged(path, size, SizeChangedValueState.Final);

			async Task<ulong> Calculate(string path, int level = 0)
			{
				if (string.IsNullOrEmpty(path))
				{
					return 0;
				}

				IntPtr hFile = Win32PInvoke.FindFirstFileExFromApp($"{path}{Path.DirectorySeparatorChar}*.*", Win32PInvoke.FINDEX_INFO_LEVELS.FindExInfoBasic,
					out Win32PInvoke.WIN32_FIND_DATA findData, Win32PInvoke.FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, Win32PInvoke.FIND_FIRST_EX_LARGE_FETCH);

				ulong size = 0;
				ulong localSize = 0;
				string localPath = string.Empty;

				if (hFile.ToInt64() is not -1)
				{
					do
					{
						if (((FileAttributes)findData.dwFileAttributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
							// Skip symbolic links and junctions
							continue;

						bool isDirectory = ((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) is FileAttributes.Directory;
						if (!isDirectory)
						{
							size += (ulong)findData.GetSize();
						}
						else if (findData.cFileName is not "." and not "..")
						{
							localPath = Path.Combine(path, findData.cFileName);
							localSize = await Calculate(localPath, level + 1);
							size += localSize;
						}

						if (level <= 3)
						{
							await Task.Yield();
							sizes[localPath] = localSize;
						}
						if (level is 0 && stopwatch.ElapsedMilliseconds > 500)
						{
							// Limit updates to every 0.5 seconds to prevent crashes due to frequent updates
							stopwatch.Restart();
							RaiseSizeChanged(path, size, SizeChangedValueState.Intermediate);
						}

						if (cancellationToken.IsCancellationRequested)
						{
							break;
						}
					} while (Win32PInvoke.FindNextFile(hFile, out findData));
					Win32PInvoke.FindClose(hFile);
				}
				return size;
			}
		}

		public bool TryGetSize(string path, out ulong size) => sizes.TryGetValue(path, out size);

		public void Dispose() { }

		private void RaiseSizeChanged(string path, ulong newSize, SizeChangedValueState valueState)
			=> SizeChanged?.Invoke(this, new SizeChangedEventArgs(path, newSize, valueState));
	}
}
