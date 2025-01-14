// Copyright (c) Files Community
// Licensed under the MIT License.

using FluentFTP;
using SevenZip;
using System;
using Windows.Storage;

namespace Files.App.Utils.Storage
{
	public interface IPasswordProtectedItem
	{
		StorageCredential Credentials { get; set; }

		Func<IPasswordProtectedItem, Task<StorageCredential>> PasswordRequestedCallback { get; set; }

		async Task<TOut> RetryWithCredentialsAsync<TOut>(Func<Task<TOut>> func, Exception exception)
		{
			var handled = exception is SevenZipOpenFailedException szofex && szofex.Result is OperationResult.WrongPassword ||
				exception is ExtractionFailedException efex && efex.Result is OperationResult.WrongPassword ||
				exception is FtpAuthenticationException;

			if (!handled || PasswordRequestedCallback is null)
				throw exception;

			Credentials = await PasswordRequestedCallback(this);

			return await func();
		}

		async Task RetryWithCredentialsAsync(Func<Task> func, Exception exception)
		{
			var handled = exception is SevenZipOpenFailedException szofex && szofex.Result is OperationResult.WrongPassword ||
				exception is ExtractionFailedException efex && efex.Result is OperationResult.WrongPassword ||
				exception is FtpAuthenticationException;

			if (!handled || PasswordRequestedCallback is null)
				throw exception;

			Credentials = await PasswordRequestedCallback(this);

			await func();
		}

		void CopyFrom(IPasswordProtectedItem parent)
		{
			Credentials = parent.Credentials;
			PasswordRequestedCallback = parent.PasswordRequestedCallback;
		}
	}
}
