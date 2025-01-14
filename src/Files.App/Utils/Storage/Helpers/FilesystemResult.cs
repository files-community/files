// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Win32.Foundation;

namespace Files.App.Utils.Storage
{
	public class FilesystemResult
	{
		public FileSystemStatusCode ErrorCode { get; }

		public FilesystemResult(FileSystemStatusCode errorCode) => ErrorCode = errorCode;

		public static implicit operator FileSystemStatusCode(FilesystemResult res) => res.ErrorCode;
		public static implicit operator FilesystemResult(FileSystemStatusCode res) => new(res);

		public static implicit operator bool(FilesystemResult res) => res?.ErrorCode is FileSystemStatusCode.Success;
		public static explicit operator FilesystemResult(bool res) => new(res ? FileSystemStatusCode.Success : FileSystemStatusCode.Generic);
		
		
		public static implicit operator BOOL(FilesystemResult res) => res?.ErrorCode is FileSystemStatusCode.Success;
		public static explicit operator FilesystemResult(BOOL res) => new(res ? FileSystemStatusCode.Success : FileSystemStatusCode.Generic);
	}

	public sealed class FilesystemResult<T> : FilesystemResult
	{
		public T Result { get; }

		public FilesystemResult(T result, FileSystemStatusCode errorCode) : base(errorCode) => Result = result;

		public static implicit operator T(FilesystemResult<T> res) => res.Result;
	}
}