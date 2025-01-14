// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.Shared.Utils
{
	/// <summary>
	/// Wraps and exposes <typeparamref name="T"/> implementation for access.
	/// </summary>
	/// <typeparam name="T">The wrapped type.</typeparam>
	public interface IWrapper<out T>
	{
		/// <summary>
		/// Gets the inner member wrapped by the implementation.
		/// </summary>
		T Inner { get; }
	}
}
