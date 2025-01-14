// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Enums
{
	/// <summary>
	/// Contains all kinds of return status
	/// </summary>
	public enum ReturnResult : byte
	{
		/// <summary>
		/// Informs that operation is still in progress
		/// </summary>
		InProgress = 0,

		/// <summary>
		/// Informs that operation completed successfully
		/// </summary>
		Success = 1,

		/// <summary>
		/// Informs that operation has failed
		/// </summary>
		Failed = 2,

		/// <summary>
		/// Informs that operation failed integrity check
		/// </summary>
		IntegrityCheckFailed = 3,

		/// <summary>
		/// Informs that operation resulted in an unknown exception
		/// </summary>
		UnknownException = 4,

		/// <summary>
		/// Informs that operation provided argument is illegal
		/// </summary>
		BadArgumentException = 5,

		/// <summary>
		/// Informs that operation provided/returned value is null
		/// </summary>
		NullException = 6,

		/// <summary>
		/// Informs that operation tried to access restricted resources
		/// </summary>
		AccessUnauthorized = 7,

		/// <summary>
		/// Informs that operation has been cancelled
		/// </summary>
		Cancelled = 8,
	}
}
