// Copyright (c) Files Community
// Licensed under the MIT License.

// Abstract:
//  Implementation of module class.

#include "pch.h"
#include "framework.h"
#include "resource.h"
#include "CustomOpenDialog_i.h"
#include "dllmain.h"

CCustomOpenDialogModule _AtlModule;

// DLL entry point
EXTERN_C BOOL WINAPI DllMain(HINSTANCE hInstance, DWORD dwReason, LPVOID lpReserved)
{
	return _AtlModule.DllMain(dwReason, lpReserved);
}
