// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Enums
{
	[Flags]
	public enum FileSystemStatusCode
	{
		/// <summary>
		/// Success
		/// </summary>
		Success = 0,

		/// <summary>
		/// Generic
		/// </summary>
		Generic = 1,

		/// <summary>
		/// Unauthorized
		/// </summary>
		Unauthorized = 2,

		/// <summary>
		/// NotFound
		/// </summary>
		NotFound = 4,

		/// <summary>
		/// InUse
		/// </summary>
		InUse = 8,

		/// <summary>
		/// NameTooLong
		/// </summary>
		NameTooLong = 16,

		/// <summary>
		/// AlreadyExists
		/// </summary>
		AlreadyExists = 32,

		/// <summary>
		/// NotAFolder
		/// </summary>
		NotAFolder = 64,

		/// <summary>
		/// NotAFile
		/// </summary>
		NotAFile = 128,

		/// <summary>
		/// ReadOnly
		/// </summary>
		ReadOnly = 256,

		/// <summary>
		/// PropertyLoss
		/// </summary>
		PropertyLoss = 512,

		/// <summary>
		/// InProgress
		/// </summary>
		InProgress = 1024,

		/// <summary>
		/// FileTooLarge
		/// </summary>
		FileTooLarge = 2048
	}
}
