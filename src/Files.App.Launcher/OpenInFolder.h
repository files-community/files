// Copyright (c) Files Community
// Licensed under the MIT License.

#pragma once

#include <iostream>
#include <objbase.h>
#include <exdisp.h>
#include <propvarutil.h>
#include <shtypes.h>
#include <ShlObj_core.h>
#include <ShObjIdl_core.h>
#include <winrt/base.h>
#include <wil/resource.h>

class OpenInFolder
{
	HWND m_hwnd;
	winrt::com_ptr<IShellWindows> m_shellWindows;

	long m_shellWindowCookie;

	HRESULT NotifyShellOfNavigation(PCIDLIST_ABSOLUTE pidl);

	std::wstring m_selectedItem;

public:
	OpenInFolder();
	~OpenInFolder();

	LRESULT CALLBACK WindowProcedure(HWND hwnd, UINT Msg, WPARAM wParam, LPARAM lParam);
	void SetWindow(HWND hwnd);
	void OnItemSelected(PIDLIST_ABSOLUTE pidl);
	void OnCreate();
	std::wstring GetResult();
};
