// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.System.WinRT;
using WinRT;

namespace Files.App.Server;

unsafe partial class Helpers
{
	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
	public static HRESULT GetActivationFactory(HSTRING activatableClassId, IActivationFactory** factory)
	{
		if (activatableClassId.IsNull || factory is null)
		{
			return HRESULT.E_INVALIDARG;
		}

		try
		{
			*factory = (IActivationFactory*)Module.GetActivationFactory(MarshalString.FromAbi((IntPtr)activatableClassId));
			return *factory is null ? HRESULT.CLASS_E_CLASSNOTAVAILABLE : HRESULT.S_OK;
		}
		catch (Exception e)
		{
			ExceptionHelpers.SetErrorInfo(e);
			return (HRESULT)ExceptionHelpers.GetHRForException(e);
		}
	}
}