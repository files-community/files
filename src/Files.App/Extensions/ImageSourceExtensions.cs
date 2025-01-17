// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Files.App.Extensions
{
	internal static class ImageSourceExtensions
	{
		internal static async Task<byte[]> ToByteArrayAsync(this IInputStream stream)
		{
			if (stream is null)
			{
				return null;
			}

			await using var readStream = stream.AsStreamForRead();

			return await readStream.ToByteArrayAsync();
		}

		internal static async Task<byte[]> ToByteArrayAsync(this StorageFile file)
		{
			if (file is null)
			{
				return null;
			}

			using (IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.Read))
			{
				var bytes = new byte[fileStream.Size];
				await fileStream.ReadAsync(bytes.AsBuffer(), (uint)fileStream.Size, InputStreamOptions.None);

				return bytes;
			}
		}

		private static async Task<byte[]> ToByteArrayAsync(this Stream stream, CancellationToken cancellationToken = default)
		{
			MemoryStream memoryStream;

			if (stream.CanSeek)
			{
				var length = stream.Length - stream.Position;
				memoryStream = new MemoryStream((int)length);
			}
			else
			{
				memoryStream = new MemoryStream();
			}

			using (memoryStream)
			{
				await stream.CopyToAsync(memoryStream, bufferSize: 81920, cancellationToken).ConfigureAwait(false);

				return memoryStream.ToArray();
			}
		}
	}
}
