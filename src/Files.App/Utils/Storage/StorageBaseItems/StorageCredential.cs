// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using System.Security;

namespace Files.App.Utils.Storage
{
	// Code from System.Net.NetworkCredential
	public sealed class StorageCredential
	{
		private string _userName = string.Empty;
		public string UserName
		{
			get => _userName;
			set => _userName = value ?? string.Empty;
		}

		private object? _password;
		public string Password
		{
			get
			{
				SecureString? sstr = _password as SecureString;
				if (sstr != null)
					return MarshalToString(sstr);

				return (string?)_password ?? string.Empty;
			}
			set
			{
				SecureString? old = _password as SecureString;
				_password = value;

				old?.Dispose();
			}
		}

		public SecureString SecurePassword
		{
			get
			{
				string? str = _password as string;
				if (str != null)
					return MarshalToSecureString(str);

				SecureString? sstr = _password as SecureString;
				return sstr != null ? sstr.Copy() : new SecureString();
			}
			set
			{
				SecureString? old = _password as SecureString;
				_password = value?.Copy();

				old?.Dispose();
			}
		}

		public StorageCredential()
			: this(string.Empty, string.Empty)
		{
		}

		public StorageCredential(string? userName, string? password)
		{
			UserName = userName;
			Password = password;
		}

		public StorageCredential(string? userName, SecureString? password)
		{
			UserName = userName;
			SecurePassword = password;
		}

		private static string MarshalToString(SecureString sstr)
		{
			if (sstr == null || sstr.Length == 0)
				return string.Empty;

			IntPtr ptr = IntPtr.Zero;
			string result = string.Empty;

			try
			{
				ptr = Marshal.SecureStringToGlobalAllocUnicode(sstr);
				result = Marshal.PtrToStringUni(ptr)!;
			}
			finally
			{
				if (ptr != IntPtr.Zero)
					Marshal.ZeroFreeGlobalAllocUnicode(ptr);
			}

			return result;
		}

		private unsafe SecureString MarshalToSecureString(string str)
		{
			if (string.IsNullOrEmpty(str))
				return new SecureString();

			fixed (char* ptr = str)
				return new SecureString(ptr, str.Length);
		}
	}
}
