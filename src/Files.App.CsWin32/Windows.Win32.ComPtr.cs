// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System;
using System.Runtime.CompilerServices;
using Windows.Win32;
using Windows.Win32.System.Com;

namespace Windows.Win32
{
	/// <summary>
	/// Contains a COM pointer and a set of methods to work with the pointer safely.
	/// </summary>
	public unsafe struct ComPtr<T> : IDisposable where T : unmanaged
	{
		private T* _ptr;

		public bool IsNull
			=> _ptr == null;

		public ComPtr(T* ptr)
		{
			_ptr = ptr;

			if (ptr is not null)
				((IUnknown*)ptr)->AddRef();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly T* Get()
		{
			return _ptr;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly T** GetAddressOf()
		{
			return (T**)Unsafe.AsPointer(ref Unsafe.AsRef(in this));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly ComPtr<U> As<U>(Guid riid) where U : unmanaged
		{
			ComPtr<U> pNewPtr = default;
			((IUnknown*)_ptr)->QueryInterface(&riid, (void**)pNewPtr.GetAddressOf());
			return pNewPtr;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Dispose()
		{
			T* ptr = _ptr;
			if (ptr is not null)
			{
				_ptr = null;
				((IUnknown*)ptr)->Release();
			}
		}
	}
}
