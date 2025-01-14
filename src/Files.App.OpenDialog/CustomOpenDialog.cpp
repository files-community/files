// Copyright (c) Files Community
// Licensed under the MIT License.

// Abstract:
//  Implementation of DLL exports.

#include "pch.h"
#include "framework.h"
#include "resource.h"
#include "CustomOpenDialog_i.h"
#include "dllmain.h"

using namespace ATL;

// Used to determine if the DLL can be downloaded by OLE.
_Use_decl_annotations_
STDAPI DllCanUnloadNow(void)
{
	return _AtlModule.DllCanUnloadNow();
}

// Returns a class factory to create an object of the requested type.
_Use_decl_annotations_
STDAPI DllGetClassObject(_In_ REFCLSID rclsid, _In_ REFIID riid, _Outptr_ LPVOID* ppv)
{
	return _AtlModule.DllGetClassObject(rclsid, riid, ppv);
}

// DllRegisterServer: Add entries to the registry.
_Use_decl_annotations_
STDAPI DllRegisterServer(void)
{
	// Registers objects, type libraries, and all type library interfaces
	HRESULT hr = _AtlModule.DllRegisterServer();
	return hr;
}

// DllUnregisterServer - Remove entries from the registry.
_Use_decl_annotations_
STDAPI DllUnregisterServer(void)
{
	HRESULT hr = _AtlModule.DllUnregisterServer();
	return hr;
}

// DllInstall - Add/Remove entries in the registry for each user on each computer.
STDAPI DllInstall(BOOL bInstall, _In_opt_  LPCWSTR pszCmdLine)
{
	HRESULT hr = E_FAIL;
	static const wchar_t szUserSwitch[] = L"user";

	if (pszCmdLine != nullptr)
	{
		if (_wcsnicmp(pszCmdLine, szUserSwitch, _countof(szUserSwitch)) == 0)
			ATL::AtlSetPerUserRegistration(true);
	}

	if (bInstall)
	{
		hr = DllRegisterServer();
		if (FAILED(hr))
			DllUnregisterServer();
	}
	else
	{
		hr = DllUnregisterServer();
	}

	return hr;
}
