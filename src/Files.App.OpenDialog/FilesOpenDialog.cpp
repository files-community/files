// Copyright (c) Files Community
// Licensed under the MIT License.

// Abstract:
//  Implementation of CFilesOpenDialog.

#include "pch.h"
#include <shlobj.h>
#include <iostream>
#include <fstream>
#include <cstdio>
#include <locale>
#include <codecvt>
#include "FilesOpenDialog.h"

//#define SYSTEMDIALOG

using std::cout;
using std::wcout;
using std::endl;

CComPtr<IFileOpenDialog> GetSystemDialog()
{
	WCHAR comdlg32Path[MAX_PATH];
	ExpandEnvironmentStringsW(L"%WINDIR%\\System32\\comdlg32.dll", comdlg32Path, MAX_PATH - 1);

	HINSTANCE lib = CoLoadLibrary(comdlg32Path, false);
	BOOL(WINAPI* dllGetClassObject)(REFCLSID, REFIID, LPVOID*) = 
		(BOOL(WINAPI*)(REFCLSID, REFIID, LPVOID*))GetProcAddress(lib, "DllGetClassObject");
	CComPtr<IClassFactory> pClassFactory;
	dllGetClassObject(CLSID_FileOpenDialog, IID_IClassFactory, (void**)&pClassFactory);
	CComPtr<IFileOpenDialog> systemDialog;
	pClassFactory->CreateInstance(NULL, IID_IFileOpenDialog, (void**)&systemDialog);
	//CoFreeLibrary(lib);
	return systemDialog;
}

IShellItem* CloneShellItem(IShellItem* psi)
{
	IShellItem* item = NULL;
	if (psi)
	{
		PIDLIST_ABSOLUTE pidl;
		if (SUCCEEDED(SHGetIDListFromObject(psi, &pidl)))
		{
			SHCreateItemFromIDList(pidl, IID_IShellItem, (void**)&item);
			CoTaskMemFree(pidl);
		}
	}

	return item;
}

std::string wstring_to_utf8_hex(const std::wstring& input)
{
	std::string output;

	int cbNeeded = WideCharToMultiByte(CP_UTF8, 0, input.c_str(), -1, NULL, 0, NULL, NULL);
	if (cbNeeded > 0)
	{
		char* utf8 = new char[cbNeeded];
		if (WideCharToMultiByte(CP_UTF8, 0, input.c_str(), -1, utf8, cbNeeded, NULL, NULL) != 0)
		{
			for (char* p = utf8; *p; p++)
			{
				char onehex[5];
				sprintf_s(onehex, sizeof(onehex), "%%%02.2X", (unsigned char)*p);
				output.append(onehex);
			}
		}

		delete[] utf8;
	}

	return output;
}

std::wstring str2wstr(const std::string& str)
{
	int cbNeeded = MultiByteToWideChar(CP_UTF8, 0, &str[0], (int)str.size(), NULL, 0);
	if (cbNeeded > 0)
	{
		std::wstring wstrTo(cbNeeded, 0);
		MultiByteToWideChar(CP_UTF8, 0, &str[0], (int)str.size(), &wstrTo[0], cbNeeded);
		return wstrTo;
	}

	return L"";
}

template <typename T>
CComPtr<T> AsInterface(CComPtr<IFileOpenDialog> dialog)
{
	CComPtr<T> dialogInterface;
	dialog->QueryInterface<T>(&dialogInterface);

	return dialogInterface;
}

CFilesOpenDialog::CFilesOpenDialog()
{
	_fos = FOS_FILEMUSTEXIST | FOS_PATHMUSTEXIST;
	_systemDialog = nullptr;
	_debugStream = NULL;
	_dialogEvents = NULL;

	PWSTR pszPath = NULL;
	HRESULT hr = SHGetKnownFolderPath(FOLDERID_Desktop, 0, NULL, &pszPath);
	if (SUCCEEDED(hr))
	{
		TCHAR debugPath[MAX_PATH];
		wsprintf(debugPath, L"%s\\%s", pszPath, L"open_dialog.txt");
#ifdef  DEBUGLOG
		_wfreopen_s(&_debugStream, debugPath, L"w", stdout);
#endif
		CoTaskMemFree(pszPath);
	}

	cout << "Create" << endl;

	TCHAR tempPath[MAX_PATH];
	GetTempPath(MAX_PATH, tempPath);
	TCHAR tempName[MAX_PATH];
	GetTempFileName(tempPath, L"fsd", 0, tempName);
	_outputPath = tempName;

	(void)SHGetKnownFolderItem(FOLDERID_Documents, KF_FLAG_DEFAULT_PATH, NULL, IID_IShellItem, (void**)&_initFolder);

	hr = _initFolder->GetDisplayName(SIGDN_NORMALDISPLAY, &pszPath);
	if (SUCCEEDED(hr))
		wcout << L"_outputPath: " << _outputPath << L", _initFolder: " << pszPath << endl;

#ifdef  SYSTEMDIALOG
	_systemDialog = GetSystemDialog();
#endif
}

void CFilesOpenDialog::FinalRelease()
{
	if (_systemDialog)
		_systemDialog.Release();

	if (_initFolder)
		_initFolder->Release();

	if (_debugStream)
		fclose(_debugStream);
}

STDAPICALL CFilesOpenDialog::Show(HWND hwndOwner)
{
	cout << "Show, hwndOwner: " << hwndOwner << endl;

#ifdef  SYSTEMDIALOG
	return _systemDialog->Show(hwndOwner);
#endif

	SHELLEXECUTEINFO ShExecInfo = { 0 };
	ShExecInfo.cbSize = sizeof(SHELLEXECUTEINFO);
	ShExecInfo.fMask = SEE_MASK_NOCLOSEPROCESS;

	PWSTR pszPath = NULL;
	WCHAR szBuf[MAX_PATH];
	TCHAR args[1024] = { 0 };
	ExpandEnvironmentStringsW(L"%LOCALAPPDATA%\\Microsoft\\WindowsApps\\files-dev.exe", szBuf, MAX_PATH - 1);

	HANDLE closeEvent = CreateEvent(NULL, FALSE, FALSE, TEXT("FILEDIALOG"));

	if (_initFolder && SUCCEEDED(_initFolder->GetDisplayName(SIGDN_DESKTOPABSOLUTEPARSING, &pszPath)))
	{
		swprintf(args, _countof(args) - 1, L"\"%s\" -directory \"%s\" -outputpath \"%s\"", szBuf, pszPath, _outputPath.c_str());
		wcout << L"Invoking: " << args << endl;
		CoTaskMemFree(pszPath);
	}
	else
	{
		swprintf(args, _countof(args) - 1, L"\"%s\" -outputpath \"%s\"", szBuf, _outputPath.c_str());
	}

	std::wstring uriWithArgs = L"files-dev:?cmd=" + str2wstr(wstring_to_utf8_hex(args));
	ShExecInfo.lpFile = uriWithArgs.c_str();
	ShExecInfo.nShow = SW_SHOW;
	ShellExecuteEx(&ShExecInfo);

	if (hwndOwner)
		EnableWindow(hwndOwner, FALSE);

	MSG msg;
	while (ShExecInfo.hProcess)
	{
		switch (MsgWaitForMultipleObjectsEx(1, &closeEvent, INFINITE, QS_ALLINPUT, 0))
		{
		case WAIT_OBJECT_0:
			CloseHandle(ShExecInfo.hProcess);
			ShExecInfo.hProcess = NULL;
			break;
		case WAIT_OBJECT_0 + 1:
			while (PeekMessage(&msg, 0, 0, 0, PM_REMOVE))
			{
				TranslateMessage(&msg);
				DispatchMessage(&msg);
			}
			continue;
		default: __debugbreak();
		}
	}

	if (closeEvent)
		CloseHandle(closeEvent);

	if (hwndOwner)
	{
		EnableWindow(hwndOwner, TRUE);
		SetForegroundWindow(hwndOwner);
	}

	std::ifstream file(_outputPath);
	if (file.good())
	{
		std::string str;
		std::wstring_convert<std::codecvt_utf8_utf16<wchar_t>> converter;

		while (std::getline(file, str))
		{
			std::wstring wide = converter.from_bytes(str);
			_selectedItems.push_back(wide);
		}
	}

	DeleteFile(_outputPath.c_str());

	if (!_selectedItems.empty())
	{
		if (_dialogEvents)
			_dialogEvents->OnFileOk(this);
	}

	return !_selectedItems.empty() ? S_OK : HRESULT_FROM_WIN32(ERROR_CANCELLED);
}

STDAPICALL CFilesOpenDialog::SetFileTypes(UINT cFileTypes, const COMDLG_FILTERSPEC* rgFilterSpec)
{
	cout << "SetFileTypes, cFileTypes: " << cFileTypes << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->SetFileTypes(cFileTypes, rgFilterSpec);
#endif
	return S_OK;
}

STDAPICALL CFilesOpenDialog::SetFileTypeIndex(UINT iFileType)
{
	cout << "SetFileTypeIndex, iFileType: " << iFileType << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->SetFileTypeIndex(iFileType);
#endif
	return S_OK;
}

STDAPICALL CFilesOpenDialog::GetFileTypeIndex(UINT* piFileType)
{
	cout << "GetFileTypeIndex" << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->GetFileTypeIndex(piFileType);
#endif
	* piFileType = 1;
	return S_OK;
}

STDAPICALL CFilesOpenDialog::Advise(IFileDialogEvents* pfde, DWORD* pdwCookie)
{
	cout << "Advise" << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->Advise(pfde, pdwCookie);
#endif
	_dialogEvents = pfde;
	*pdwCookie = 0;
	return S_OK;
}

STDAPICALL CFilesOpenDialog::Unadvise(DWORD dwCookie)
{
	cout << "Unadvise, dwCookie: " << dwCookie << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->Unadvise(dwCookie);
#endif
	_dialogEvents = NULL;
	return S_OK;
}

STDAPICALL CFilesOpenDialog::SetOptions(FILEOPENDIALOGOPTIONS fos)
{
	cout << "SetOptions, fos: " << fos << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->SetOptions(fos);
#endif
	_fos = fos;
	return S_OK;
}

STDAPICALL CFilesOpenDialog::GetOptions(FILEOPENDIALOGOPTIONS* pfos)
{
	cout << "GetOptions, fos: " << _fos << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->GetOptions(pfos);
#endif
	* pfos = _fos;
	return S_OK;
}

STDAPICALL CFilesOpenDialog::SetDefaultFolder(IShellItem* psi)
{
	PWSTR pszPath = NULL;
	if (SUCCEEDED(psi->GetDisplayName(SIGDN_DESKTOPABSOLUTEPARSING, &pszPath)))
	{
		wcout << L"SetDefaultFolder, psi: " << pszPath << endl;
		CoTaskMemFree(pszPath);
	}
#ifdef SYSTEMDIALOG
	return _systemDialog->SetDefaultFolder(psi);
#endif
	if (_initFolder)
	{
		_initFolder->Release();
	}
	_initFolder = CloneShellItem(psi);
	return S_OK;
}

STDAPICALL CFilesOpenDialog::SetFolder(IShellItem* psi)
{
	PWSTR pszPath = NULL;
	if (SUCCEEDED(psi->GetDisplayName(SIGDN_DESKTOPABSOLUTEPARSING, &pszPath)))
	{
		wcout << L"SetFolder, psi: " << pszPath << endl;
		CoTaskMemFree(pszPath);
	}
#ifdef SYSTEMDIALOG
	return _systemDialog->SetFolder(psi);
#endif
	if (_initFolder)
	{
		_initFolder->Release();
	}
	_initFolder = CloneShellItem(psi);
	return S_OK;
}

STDAPICALL CFilesOpenDialog::GetFolder(IShellItem** ppsi)
{
	cout << "GetFolder" << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->GetFolder(ppsi);
#endif
	* ppsi = NULL;
	return E_NOTIMPL;
}

STDAPICALL CFilesOpenDialog::GetCurrentSelection(IShellItem** ppsi)
{
	cout << "GetCurrentSelection" << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->GetCurrentSelection(ppsi);
#endif
	return GetResult(ppsi);
}

STDAPICALL CFilesOpenDialog::SetFileName(LPCWSTR pszName)
{
	wcout << L"SetFileName, pszName: " << pszName << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->SetFileName(pszName);
#endif
	return S_OK;
}

STDAPICALL CFilesOpenDialog::GetFileName(LPWSTR* pszName)
{
	cout << "GetFileName" << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->GetFileName(pszName);
#endif
	SHStrDupW(L"", pszName);
	if (!_selectedItems.empty())
	{
		SHStrDupW(_selectedItems[0].c_str(), pszName);
	}
	return S_OK;
}

STDAPICALL CFilesOpenDialog::SetTitle(LPCWSTR pszTitle)
{
	cout << "SetTitle, title: " << pszTitle << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->SetTitle(pszTitle);
#endif
	return S_OK;
}

STDAPICALL CFilesOpenDialog::SetOkButtonLabel(LPCWSTR pszText)
{
	cout << "SetOkButtonLabel, pszText: " << pszText << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->SetOkButtonLabel(pszText);
#endif
	return S_OK;
}

STDAPICALL CFilesOpenDialog::SetFileNameLabel(LPCWSTR pszLabel)
{
	cout << "SetFileNameLabel, pszLabel: " << pszLabel << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->SetFileNameLabel(pszLabel);
#endif
	return S_OK;
}

STDAPICALL CFilesOpenDialog::GetResult(IShellItem** ppsi)
{
	cout << "GetResult" << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->GetResult(ppsi);
#endif
	if (!_selectedItems.empty())
	{
		SHCreateItemFromParsingName(_selectedItems[0].c_str(), NULL, IID_IShellItem, (void**)ppsi);
		return S_OK;
	}
	return E_NOTIMPL;
}

STDAPICALL CFilesOpenDialog::AddPlace(IShellItem* psi, FDAP fdap)
{
	PWSTR pszPath = NULL;
	if (SUCCEEDED(psi->GetDisplayName(SIGDN_DESKTOPABSOLUTEPARSING, &pszPath)))
	{
		wcout << L"AddPlace, psi: " << pszPath << endl;
		CoTaskMemFree(pszPath);
	}
#ifdef SYSTEMDIALOG
	return _systemDialog->AddPlace(psi, fdap);
#endif
	return S_OK;
}

STDAPICALL CFilesOpenDialog::SetDefaultExtension(LPCWSTR pszDefaultExtension)
{
	cout << "SetDefaultExtension, pszDefaultExtension: " << pszDefaultExtension << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->SetDefaultExtension(pszDefaultExtension);
#endif
	return S_OK;
}

STDAPICALL CFilesOpenDialog::Close(HRESULT hr)
{
	cout << "Close, hr: " << hr << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->Close(hr);
#endif
	return E_NOTIMPL;
}

STDAPICALL CFilesOpenDialog::SetClientGuid(REFGUID guid)
{
	cout << "SetClientGuid" << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->SetClientGuid(guid);
#endif
	return S_OK;
}

STDAPICALL CFilesOpenDialog::ClearClientData(void)
{
	cout << "ClearClientData" << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->ClearClientData();
#endif
	return S_OK;
}

STDAPICALL CFilesOpenDialog::SetFilter(IShellItemFilter* pFilter)
{
	cout << "SetFilter" << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->SetFilter(pFilter);
#endif
	return S_OK;
}

STDAPICALL CFilesOpenDialog::GetResults(IShellItemArray** ppenum)
{
	cout << "GetResults, results: " << _selectedItems.size() << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->GetResults(ppenum);
#endif
	if (!_selectedItems.empty())
	{
		std::vector<PIDLIST_ABSOLUTE> pidls;
		IShellItem* psi;
		PIDLIST_ABSOLUTE pidl;
		for (std::wstring ipath : _selectedItems)
		{
			if (SUCCEEDED(SHCreateItemFromParsingName(ipath.c_str(), NULL, IID_IShellItem, (void**)&psi)))
			{
				if (SUCCEEDED(SHGetIDListFromObject(psi, &pidl)))
				{
					pidls.push_back(ILClone(pidl));
				}
				psi->Release();
			}
		}

		HRESULT hr = SHCreateShellItemArrayFromIDLists((UINT)pidls.size(), (LPCITEMIDLIST*)&pidls[0], ppenum);
		for (PIDLIST_ABSOLUTE item : pidls)
		{
			CoTaskMemFree(item);
		}
		return hr;
	}
	return E_NOTIMPL;
}

STDAPICALL CFilesOpenDialog::GetSelectedItems(IShellItemArray** ppsai)
{
	cout << "GetSelectedItems" << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->GetSelectedItems(ppsai);
#endif
	return GetResults(ppsai);
}

STDAPICALL CFilesOpenDialog::EnableOpenDropDown(DWORD dwIDCtl)
{
	cout << "EnableOpenDropDown" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->EnableOpenDropDown(dwIDCtl);
#endif
	return S_OK;
}

STDAPICALL CFilesOpenDialog::AddMenu(DWORD dwIDCtl, LPCWSTR pszLabel)
{
	cout << "AddMenu" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->AddMenu(dwIDCtl, pszLabel);
#endif
	return S_OK;
}

STDAPICALL CFilesOpenDialog::AddPushButton(DWORD dwIDCtl, LPCWSTR pszLabel)
{
	cout << "AddPushButton" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->AddPushButton(dwIDCtl, pszLabel);
#endif
	return S_OK;
}

STDAPICALL CFilesOpenDialog::AddComboBox(DWORD dwIDCtl)
{
	cout << "AddComboBox" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->AddComboBox(dwIDCtl);
#endif
	return S_OK;
}

STDAPICALL CFilesOpenDialog::AddRadioButtonList(DWORD dwIDCtl)
{
	cout << "AddRadioButtonList" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->AddRadioButtonList(dwIDCtl);
#endif
	return S_OK;
}

STDAPICALL CFilesOpenDialog::AddCheckButton(DWORD dwIDCtl, LPCWSTR pszLabel, BOOL bChecked)
{
	cout << "AddCheckButton" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->AddCheckButton(dwIDCtl, pszLabel, bChecked);
#endif
	return S_OK;
}

STDAPICALL CFilesOpenDialog::AddEditBox(DWORD dwIDCtl, LPCWSTR pszText)
{
	cout << "AddEditBox" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->AddEditBox(dwIDCtl, pszText);
#endif
	return S_OK;
}

STDAPICALL CFilesOpenDialog::AddSeparator(DWORD dwIDCtl)
{
	cout << "AddSeparator" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->AddSeparator(dwIDCtl);
#endif
	return S_OK;
}

STDAPICALL CFilesOpenDialog::AddText(DWORD dwIDCtl, LPCWSTR pszText)
{
	cout << "AddText" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->AddText(dwIDCtl, pszText);
#endif
	return S_OK;
}

STDAPICALL CFilesOpenDialog::SetControlLabel(DWORD dwIDCtl, LPCWSTR pszLabel)
{
	cout << "SetControlLabel" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->SetControlLabel(dwIDCtl, pszLabel);
#endif
	return S_OK;
}

STDAPICALL CFilesOpenDialog::GetControlState(DWORD dwIDCtl, CDCONTROLSTATEF* pdwState)
{
	cout << "GetControlState" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->GetControlState(dwIDCtl, pdwState);
#endif
	* pdwState = CDCS_ENABLEDVISIBLE;
	return S_OK;
}

STDAPICALL CFilesOpenDialog::SetControlState(DWORD dwIDCtl, CDCONTROLSTATEF dwState)
{
	cout << "SetControlState" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->SetControlState(dwIDCtl, dwState);
#endif
	return S_OK;
}

STDAPICALL CFilesOpenDialog::GetEditBoxText(DWORD dwIDCtl, WCHAR** ppszText)
{
	cout << "GetEditBoxText" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->GetEditBoxText(dwIDCtl, ppszText);
#endif
	SHStrDupW(L"", ppszText);
	return S_OK;
}

STDAPICALL CFilesOpenDialog::SetEditBoxText(DWORD dwIDCtl, LPCWSTR pszText)
{
	cout << "SetEditBoxText" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->SetEditBoxText(dwIDCtl, pszText);
#endif
	return S_OK;
}

STDAPICALL CFilesOpenDialog::GetCheckButtonState(DWORD dwIDCtl, BOOL* pbChecked)
{
	cout << "GetCheckButtonState" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->GetCheckButtonState(dwIDCtl, pbChecked);
#endif
	* pbChecked = false;
	return S_OK;
}

STDAPICALL CFilesOpenDialog::SetCheckButtonState(DWORD dwIDCtl, BOOL bChecked)
{
	cout << "SetCheckButtonState" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->SetCheckButtonState(dwIDCtl, bChecked);
#endif
	return S_OK;
}

STDAPICALL CFilesOpenDialog::AddControlItem(DWORD dwIDCtl, DWORD dwIDItem, LPCWSTR pszLabel)
{
	cout << "AddControlItem" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->AddControlItem(dwIDCtl, dwIDItem, pszLabel);
#endif
	return S_OK;
}

STDAPICALL CFilesOpenDialog::RemoveControlItem(DWORD dwIDCtl, DWORD dwIDItem)
{
	cout << "RemoveControlItem" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->RemoveControlItem(dwIDCtl, dwIDItem);
#endif
	return S_OK;
}

STDAPICALL CFilesOpenDialog::RemoveAllControlItems(DWORD dwIDCtl)
{
	cout << "RemoveAllControlItems" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->RemoveAllControlItems(dwIDCtl);
#endif
	return S_OK;
}

STDAPICALL CFilesOpenDialog::GetControlItemState(DWORD dwIDCtl, DWORD dwIDItem, CDCONTROLSTATEF* pdwState)
{
	cout << "GetControlItemState" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->GetControlItemState(dwIDCtl, dwIDItem, pdwState);
#endif
	* pdwState = CDCS_ENABLEDVISIBLE;
	return S_OK;
}

STDAPICALL CFilesOpenDialog::SetControlItemState(DWORD dwIDCtl, DWORD dwIDItem, CDCONTROLSTATEF dwState)
{
	cout << "SetControlItemState" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->SetControlItemState(dwIDCtl, dwIDItem, dwState);
#endif
	return S_OK;
}

STDAPICALL CFilesOpenDialog::GetSelectedControlItem(DWORD dwIDCtl, DWORD* pdwIDItem)
{
	cout << "GetSelectedControlItem" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->GetSelectedControlItem(dwIDCtl, pdwIDItem);
#endif
	* pdwIDItem = 0;
	return S_OK;
}

STDAPICALL CFilesOpenDialog::SetSelectedControlItem(DWORD dwIDCtl, DWORD dwIDItem)
{
	cout << "SetSelectedControlItem" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->SetSelectedControlItem(dwIDCtl, dwIDItem);
#endif
	return S_OK;
}

STDAPICALL CFilesOpenDialog::StartVisualGroup(DWORD dwIDCtl, LPCWSTR pszLabel)
{
	cout << "StartVisualGroup" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->StartVisualGroup(dwIDCtl, pszLabel);
#endif
	return S_OK;
}

STDAPICALL CFilesOpenDialog::EndVisualGroup(void)
{
	cout << "EndVisualGroup" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->EndVisualGroup();
#endif
	return S_OK;
}

STDAPICALL CFilesOpenDialog::MakeProminent(DWORD dwIDCtl)
{
	cout << "MakeProminent" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->MakeProminent(dwIDCtl);
#endif
	return S_OK;
}

STDAPICALL CFilesOpenDialog::SetControlItemText(DWORD dwIDCtl, DWORD dwIDItem, LPCWSTR pszLabel)
{
	cout << "SetControlItemText" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->SetControlItemText(dwIDCtl, dwIDItem, pszLabel);
#endif
	return S_OK;
}

STDAPICALL CFilesOpenDialog::SetCancelButtonLabel(LPCWSTR pszLabel)
{
	cout << "SetCancelButtonLabel" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialog2>(_systemDialog)->SetCancelButtonLabel(pszLabel);
#endif
	return S_OK;
}

STDAPICALL CFilesOpenDialog::SetNavigationRoot(IShellItem* psi)
{
	cout << "SetNavigationRoot" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialog2>(_systemDialog)->SetNavigationRoot(psi);
#endif
	return S_OK;
}

STDAPICALL CFilesOpenDialog::SetSite(IUnknown* pUnkSite)
{
	cout << "SetSite" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IObjectWithSite>(_systemDialog)->SetSite(pUnkSite);
#endif
	return S_OK;
}

STDAPICALL CFilesOpenDialog::GetSite(REFIID riid, void** ppvSite)
{
	cout << "GetSite" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IObjectWithSite>(_systemDialog)->GetSite(riid, ppvSite);
#endif
	return E_NOTIMPL;
}

STDAPICALL CFilesOpenDialog::HideControlsForHostedPickerProviderApp(void)
{
	cout << "HideControlsForHostedPickerProviderApp" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->HideControlsForHostedPickerProviderApp();
#endif
	return E_NOTIMPL;
}

STDAPICALL CFilesOpenDialog::EnableControlsForHostedPickerProviderApp(void)
{
	cout << "EnableControlsForHostedPickerProviderApp" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->EnableControlsForHostedPickerProviderApp();
#endif
	return E_NOTIMPL;
}

STDAPICALL CFilesOpenDialog::GetPrivateOptions(unsigned long* pfos)
{
	cout << "GetPrivateOptions" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->GetPrivateOptions(pfos);
#endif
	* pfos = 0;
	return S_OK;
}

STDAPICALL CFilesOpenDialog::SetPrivateOptions(unsigned long fos)
{
	cout << "SetPrivateOptions" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->SetPrivateOptions(fos);
#endif
	return S_OK;
}

STDAPICALL CFilesOpenDialog::SetPersistenceKey(unsigned short const* pkey)
{
	cout << "SetPersistenceKey" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->SetPersistenceKey(pkey);
#endif
	return E_NOTIMPL;
}

STDAPICALL CFilesOpenDialog::HasPlaces(void)
{
	cout << "HasPlaces" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->HasPlaces();
#endif
	return E_NOTIMPL;
}

STDAPICALL CFilesOpenDialog::EnumPlaces(int plc, _GUID const& riid, void** ppv)
{
	cout << "EnumPlaces" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->EnumPlaces(plc, riid, ppv);
#endif
	return E_NOTIMPL;
}

STDAPICALL CFilesOpenDialog::EnumControls(void** ppv)
{
	cout << "EnumControls" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->EnumControls(ppv);
#endif
	return E_NOTIMPL;
}

STDAPICALL CFilesOpenDialog::GetPersistRegkey(unsigned short** preg)
{
	cout << "GetPersistRegkey" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->GetPersistRegkey(preg);
#endif
	return E_NOTIMPL;
}

STDAPICALL CFilesOpenDialog::GetSavePropertyStore(IPropertyStore** ppstore, IPropertyDescriptionList** ppdesclist)
{
	cout << "GetSavePropertyStore" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->GetSavePropertyStore(ppstore, ppdesclist);
#endif
	return E_NOTIMPL;
}

STDAPICALL CFilesOpenDialog::GetSaveExtension(unsigned short** pext)
{
	cout << "GetSaveExtension" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->GetSaveExtension(pext);
#endif
	return E_NOTIMPL;
}

STDAPICALL CFilesOpenDialog::GetFileTypeControl(void** ftp)
{
	cout << "GetFileTypeControl" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->GetFileTypeControl(ftp);
#endif
	return E_NOTIMPL;
}

STDAPICALL CFilesOpenDialog::GetFileNameControl(void** pctrl)
{
	cout << "GetFileNameControl" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->GetFileNameControl(pctrl);
#endif
	return E_NOTIMPL;
}

STDAPICALL CFilesOpenDialog::GetFileProtectionControl(void** pfctrl)
{
	cout << "GetFileProtectionControl" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->GetFileProtectionControl(pfctrl);
#endif
	return E_NOTIMPL;
}

STDAPICALL CFilesOpenDialog::SetFolderPrivate(IShellItem* psi, int arg)
{
	cout << "SetFolderPrivate" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->SetFolderPrivate(psi, arg);
#endif
	return E_NOTIMPL;
}

STDAPICALL CFilesOpenDialog::SetCustomControlAreaHeight(unsigned int height)
{
	cout << "SetCustomControlAreaHeight" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->SetCustomControlAreaHeight(height);
#endif
	return E_NOTIMPL;
}

STDAPICALL CFilesOpenDialog::GetDialogState(unsigned long arg, unsigned long* pstate)
{
	cout << "GetDialogState" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->GetDialogState(arg, pstate);
#endif
	return E_NOTIMPL;
}

STDAPICALL CFilesOpenDialog::SetAppControlsModule(void* papp)
{
	cout << "SetAppControlsModule" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->SetAppControlsModule(papp);
#endif
	return E_NOTIMPL;
}

STDAPICALL CFilesOpenDialog::SetUserEditedSaveProperties(void)
{
	cout << "SetUserEditedSaveProperties" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->SetUserEditedSaveProperties();
#endif
	return E_NOTIMPL;
}

STDAPICALL CFilesOpenDialog::ShouldShowStandardNavigationRoots(void)
{
	cout << "ShouldShowStandardNavigationRoots" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->ShouldShowStandardNavigationRoots();
#endif
	return E_NOTIMPL;
}

STDAPICALL CFilesOpenDialog::GetNavigationRoot(_GUID const& riid, void** ppv)
{
	cout << "GetNavigationRoot" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->GetNavigationRoot(riid, ppv);
#endif
	return E_NOTIMPL;
}

STDAPICALL CFilesOpenDialog::ShouldShowFileProtectionControl(int* pfpc)
{
	cout << "ShouldShowFileProtectionControl" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->ShouldShowFileProtectionControl(pfpc);
#endif
	return E_NOTIMPL;
}

STDAPICALL CFilesOpenDialog::GetCurrentDialogView(_GUID const& riid, void** ppv)
{
	cout << "GetCurrentDialogView" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->GetCurrentDialogView(riid, ppv);
#endif
	return E_NOTIMPL;
}

STDAPICALL CFilesOpenDialog::SetSaveDialogEditBoxTextAndFileType(int arg, unsigned short const* pargb)
{
	cout << "SetSaveDialogEditBoxTextAndFileType" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->SetSaveDialogEditBoxTextAndFileType(arg, pargb);
#endif
	return E_NOTIMPL;
}

STDAPICALL CFilesOpenDialog::MoveFocusFromBrowser(int arg)
{
	cout << "MoveFocusFromBrowser" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->MoveFocusFromBrowser(arg);
#endif
	return E_NOTIMPL;
}

STDAPICALL CFilesOpenDialog::EnableOkButton(int enbl)
{
	cout << "EnableOkButton" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->EnableOkButton(enbl);
#endif
	return E_NOTIMPL;
}

STDAPICALL CFilesOpenDialog::InitEnterpriseId(unsigned short const* pid)
{
	cout << "InitEnterpriseId" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->InitEnterpriseId(pid);
#endif
	return E_NOTIMPL;
}

STDAPICALL CFilesOpenDialog::AdviseFirst(IFileDialogEvents* pfde, unsigned long* pdwCookie)
{
	cout << "AdviseFirst" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->AdviseFirst(pfde, pdwCookie);
#endif
	return E_NOTIMPL;
}

STDAPICALL CFilesOpenDialog::HandleTab(void)
{
	cout << "HandleTab" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->HandleTab();
#endif
	return E_NOTIMPL;
}
