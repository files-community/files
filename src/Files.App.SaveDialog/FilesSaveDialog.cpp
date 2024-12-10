// FilesSaveDialog.cpp: implementazione di CFilesSaveDialog

#include "pch.h"
#include "FilesDialogEvents.h"
#include "FilesSaveDialog.h"
#include <shlobj.h>
#include <iostream>
#include <fstream>
#include <cstdio>
#include <locale>
#include <codecvt>

//#define SYSTEMDIALOG

using std::cout;
using std::wcout;
using std::endl;

// CFilesSaveDialog

CComPtr<IFileSaveDialog> GetSystemDialog()
{
	WCHAR comdlg32Path[MAX_PATH];
	ExpandEnvironmentStringsW(L"%WINDIR%\\System32\\comdlg32.dll", comdlg32Path, MAX_PATH - 1);

	HINSTANCE lib = CoLoadLibrary(comdlg32Path, false);
	BOOL(WINAPI* dllGetClassObject)(REFCLSID, REFIID, LPVOID*) =
		(BOOL(WINAPI*)(REFCLSID, REFIID, LPVOID*))GetProcAddress(lib, "DllGetClassObject");
	CComPtr<IClassFactory> pClassFactory;
	dllGetClassObject(CLSID_FileSaveDialog, IID_IClassFactory, (void**)&pClassFactory);
	CComPtr<IFileSaveDialog> systemDialog;
	pClassFactory->CreateInstance(NULL, IID_IFileSaveDialog, (void**)&systemDialog);
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
CComPtr<T> AsInterface(CComPtr<IFileSaveDialog> dialog)
{
	CComPtr<T> dialogInterface;
	dialog->QueryInterface<T>(&dialogInterface);
	return dialogInterface;
}

CFilesSaveDialog::CFilesSaveDialog()
{
	_fos = FOS_PATHMUSTEXIST;
	_systemDialog = nullptr;
	_debugStream = NULL;
	_dialogEvents = NULL;

	PWSTR pszPath = NULL;
	HRESULT hr = SHGetKnownFolderPath(FOLDERID_Desktop, 0, NULL, &pszPath);
	if (SUCCEEDED(hr))
	{
		TCHAR debugPath[MAX_PATH];
		wsprintf(debugPath, L"%s\\%s", pszPath, L"save_dialog.txt");
#ifdef DEBUGLOG
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
	{
		wcout << L"_outputPath: " << _outputPath << L", _initFolder: " << pszPath << endl;
	}

#ifdef SYSTEMDIALOG
	_systemDialog = GetSystemDialog();
#endif
}

void CFilesSaveDialog::FinalRelease()
{
	if (_systemDialog)
	{
		_systemDialog.Release();
	}
	if (_initFolder)
	{
		_initFolder->Release();
	}
	if (_debugStream)
	{
		fclose(_debugStream);
	}
}

HRESULT __stdcall CFilesSaveDialog::SetSite(IUnknown* pUnkSite)
{
	cout << "SetSite" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IObjectWithSite>(_systemDialog)->SetSite(pUnkSite);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::GetSite(REFIID riid, void** ppvSite)
{
	cout << "GetSite" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IObjectWithSite>(_systemDialog)->GetSite(riid, ppvSite);
#endif
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::EnableOpenDropDown(DWORD dwIDCtl)
{
	cout << "EnableOpenDropDown" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->EnableOpenDropDown(dwIDCtl);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::AddMenu(DWORD dwIDCtl, LPCWSTR pszLabel)
{
	cout << "AddMenu" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->AddMenu(dwIDCtl, pszLabel);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::AddPushButton(DWORD dwIDCtl, LPCWSTR pszLabel)
{
	cout << "AddPushButton" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->AddPushButton(dwIDCtl, pszLabel);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::AddComboBox(DWORD dwIDCtl)
{
	cout << "AddComboBox" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->AddComboBox(dwIDCtl);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::AddRadioButtonList(DWORD dwIDCtl)
{
	cout << "AddRadioButtonList" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->AddRadioButtonList(dwIDCtl);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::AddCheckButton(DWORD dwIDCtl, LPCWSTR pszLabel, BOOL bChecked)
{
	cout << "AddCheckButton" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->AddCheckButton(dwIDCtl, pszLabel, bChecked);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::AddEditBox(DWORD dwIDCtl, LPCWSTR pszText)
{
	cout << "AddEditBox" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->AddEditBox(dwIDCtl, pszText);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::AddSeparator(DWORD dwIDCtl)
{
	cout << "AddSeparator" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->AddSeparator(dwIDCtl);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::AddText(DWORD dwIDCtl, LPCWSTR pszText)
{
	cout << "AddText" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->AddText(dwIDCtl, pszText);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::SetControlLabel(DWORD dwIDCtl, LPCWSTR pszLabel)
{
	cout << "SetControlLabel" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->SetControlLabel(dwIDCtl, pszLabel);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::GetControlState(DWORD dwIDCtl, CDCONTROLSTATEF* pdwState)
{
	cout << "GetControlState" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->GetControlState(dwIDCtl, pdwState);
#endif
	* pdwState = CDCS_ENABLEDVISIBLE;
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::SetControlState(DWORD dwIDCtl, CDCONTROLSTATEF dwState)
{
	cout << "SetControlState" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->SetControlState(dwIDCtl, dwState);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::GetEditBoxText(DWORD dwIDCtl, WCHAR** ppszText)
{
	cout << "GetEditBoxText" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->GetEditBoxText(dwIDCtl, ppszText);
#endif
	SHStrDupW(L"", ppszText);
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::SetEditBoxText(DWORD dwIDCtl, LPCWSTR pszText)
{
	cout << "SetEditBoxText" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->SetEditBoxText(dwIDCtl, pszText);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::GetCheckButtonState(DWORD dwIDCtl, BOOL* pbChecked)
{
	cout << "GetCheckButtonState" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->GetCheckButtonState(dwIDCtl, pbChecked);
#endif
	* pbChecked = false;
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::SetCheckButtonState(DWORD dwIDCtl, BOOL bChecked)
{
	cout << "SetCheckButtonState" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->SetCheckButtonState(dwIDCtl, bChecked);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::AddControlItem(DWORD dwIDCtl, DWORD dwIDItem, LPCWSTR pszLabel)
{
	cout << "AddControlItem" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->AddControlItem(dwIDCtl, dwIDItem, pszLabel);
#endif
	_ctrlItems.push_back(dwIDItem);
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::RemoveControlItem(DWORD dwIDCtl, DWORD dwIDItem)
{
	cout << "RemoveControlItem" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->RemoveControlItem(dwIDCtl, dwIDItem);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::RemoveAllControlItems(DWORD dwIDCtl)
{
	cout << "RemoveAllControlItems" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->RemoveAllControlItems(dwIDCtl);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::GetControlItemState(DWORD dwIDCtl, DWORD dwIDItem, CDCONTROLSTATEF* pdwState)
{
	cout << "GetControlItemState" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->GetControlItemState(dwIDCtl, dwIDItem, pdwState);
#endif
	* pdwState = CDCS_ENABLEDVISIBLE;
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::SetControlItemState(DWORD dwIDCtl, DWORD dwIDItem, CDCONTROLSTATEF dwState)
{
	cout << "SetControlItemState" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->SetControlItemState(dwIDCtl, dwIDItem, dwState);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::GetSelectedControlItem(DWORD dwIDCtl, DWORD* pdwIDItem)
{
	cout << "GetSelectedControlItem" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->GetSelectedControlItem(dwIDCtl, pdwIDItem);
#endif
	if (!_ctrlItems.empty()) {
		*pdwIDItem = _ctrlItems.back();
		return S_OK;
	}
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::SetSelectedControlItem(DWORD dwIDCtl, DWORD dwIDItem)
{
	cout << "SetSelectedControlItem" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->SetSelectedControlItem(dwIDCtl, dwIDItem);
#endif
	_ctrlItems.push_back(dwIDItem);
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::StartVisualGroup(DWORD dwIDCtl, LPCWSTR pszLabel)
{
	cout << "StartVisualGroup" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->StartVisualGroup(dwIDCtl, pszLabel);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::EndVisualGroup(void)
{
	cout << "EndVisualGroup" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->EndVisualGroup();
#endif
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::MakeProminent(DWORD dwIDCtl)
{
	cout << "MakeProminent" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->MakeProminent(dwIDCtl);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::SetControlItemText(DWORD dwIDCtl, DWORD dwIDItem, LPCWSTR pszLabel)
{
	cout << "SetControlItemText" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogCustomize>(_systemDialog)->SetControlItemText(dwIDCtl, dwIDItem, pszLabel);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::Show(HWND hwndOwner)
{
	wchar_t wnd_title[1024];
	GetWindowText(hwndOwner, wnd_title, 1024);
	wcout << L"Show, ID: " << GetCurrentProcessId() << endl;

#ifdef SYSTEMDIALOG
	HRESULT res = _systemDialog->Show(NULL);
	cout << "Show, DONE: " << res << endl;
	return res;
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
		if (!_initName.empty())
		{
			swprintf(args, _countof(args) - 1, L"\"%s\" -directory \"%s\" -outputpath \"%s\" -select \"%s\"", szBuf, pszPath, _outputPath.c_str(), _initName.c_str());
		}
		else
		{
			swprintf(args, _countof(args) - 1, L"\"%s\" -directory \"%s\" -outputpath \"%s\"", szBuf, pszPath, _outputPath.c_str());
		}
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
			_selectedItem = wide;
		}
	}
	DeleteFile(_outputPath.c_str());

	if (!_selectedItem.empty())
	{
		// Create destination file if not existing
		HANDLE hFile = CreateFile(_selectedItem.c_str(), GENERIC_WRITE, 0, 0, OPEN_ALWAYS, FILE_ATTRIBUTE_NORMAL, 0);
		if (hFile != INVALID_HANDLE_VALUE)
		{
			CloseHandle(hFile);
		}
		else
		{
			_selectedItem = L"";
		}
	}
	if (!_selectedItem.empty())
	{
		if (_dialogEvents)
		{
			_dialogEvents->OnFileOk(this);
		}
	}

	return !_selectedItem.empty() ? S_OK : HRESULT_FROM_WIN32(ERROR_CANCELLED);
}

HRESULT __stdcall CFilesSaveDialog::SetFileTypes(UINT cFileTypes, const COMDLG_FILTERSPEC* rgFilterSpec)
{
	cout << "SetFileTypes, cFileTypes: " << cFileTypes << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->SetFileTypes(cFileTypes, rgFilterSpec);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::SetFileTypeIndex(UINT iFileType)
{
	cout << "SetFileTypeIndex, iFileType: " << iFileType << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->SetFileTypeIndex(iFileType);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::GetFileTypeIndex(UINT* piFileType)
{
	cout << "GetFileTypeIndex" << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->GetFileTypeIndex(piFileType);
#endif
	* piFileType = 1;
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::Advise(IFileDialogEvents* pfde, DWORD* pdwCookie)
{
	cout << "Advise" << endl;
#ifdef DEBUGLOG
	pfde = new FilesDialogEvents(pfde, this);
#endif
#ifdef SYSTEMDIALOG
	return _systemDialog->Advise(pfde, pdwCookie);
#endif
	_dialogEvents = pfde;
	_dialogEvents->AddRef();
	*pdwCookie = 4;
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::Unadvise(DWORD dwCookie)
{
	cout << "Unadvise, dwCookie: " << dwCookie << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->Unadvise(dwCookie);
#endif
	_dialogEvents->Release();
	_dialogEvents = NULL;
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::SetOptions(FILEOPENDIALOGOPTIONS fos)
{
	cout << "SetOptions, fos: " << fos << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->SetOptions(fos);
#endif
	_fos = fos;
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::GetOptions(FILEOPENDIALOGOPTIONS* pfos)
{
	cout << "GetOptions, fos: " << _fos << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->GetOptions(pfos);
#endif
	* pfos = _fos;
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::SetDefaultFolder(IShellItem* psi)
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

HRESULT __stdcall CFilesSaveDialog::SetFolder(IShellItem* psi)
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

HRESULT __stdcall CFilesSaveDialog::GetFolder(IShellItem** ppsi)
{
	cout << "GetFolder" << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->GetFolder(ppsi);
#endif
	* ppsi = NULL;
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::GetCurrentSelection(IShellItem** ppsi)
{
	cout << "GetCurrentSelection" << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->GetCurrentSelection(ppsi);
#endif
	return GetResult(ppsi);
}

HRESULT __stdcall CFilesSaveDialog::SetFileName(LPCWSTR pszName)
{
	wcout << L"SetFileName, pszName: " << pszName << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->SetFileName(pszName);
#endif
	std::wstring absPath = std::wstring(pszName);
	_initName = absPath.substr(absPath.find_last_of(L"/\\") + 1);
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::GetFileName(LPWSTR* pszName)
{
	cout << "GetFileName" << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->GetFileName(pszName);
#endif
	SHStrDupW(L"", pszName);
	if (!_selectedItem.empty())
	{
		SHStrDupW(_selectedItem.c_str(), pszName);
	}
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::SetTitle(LPCWSTR pszTitle)
{
	wcout << L"SetTitle, title: " << pszTitle << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->SetTitle(pszTitle);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::SetOkButtonLabel(LPCWSTR pszText)
{
	wcout << L"SetOkButtonLabel, pszText: " << pszText << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->SetOkButtonLabel(pszText);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::SetFileNameLabel(LPCWSTR pszLabel)
{
	wcout << L"SetFileNameLabel, pszLabel: " << pszLabel << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->SetFileNameLabel(pszLabel);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::GetResult(IShellItem** ppsi)
{
	cout << "GetResult" << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->GetResult(ppsi);
#endif
	if (!_selectedItem.empty())
	{
		SHCreateItemFromParsingName(_selectedItem.c_str(), NULL, IID_IShellItem, (void**)ppsi);
		return S_OK;
	}
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::AddPlace(IShellItem* psi, FDAP fdap)
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

HRESULT __stdcall CFilesSaveDialog::SetDefaultExtension(LPCWSTR pszDefaultExtension)
{
	wcout << L"SetDefaultExtension, pszDefaultExtension: " << pszDefaultExtension << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->SetDefaultExtension(pszDefaultExtension);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::Close(HRESULT hr)
{
	cout << "Close, hr: " << hr << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->Close(hr);
#endif
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::SetClientGuid(REFGUID guid)
{
	cout << "SetClientGuid" << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->SetClientGuid(guid);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::ClearClientData(void)
{
	cout << "ClearClientData" << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->ClearClientData();
#endif
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::SetFilter(IShellItemFilter* pFilter)
{
	cout << "SetFilter" << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->SetFilter(pFilter);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::SetCancelButtonLabel(LPCWSTR pszLabel)
{
	cout << "SetCancelButtonLabel" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialog2>(_systemDialog)->SetCancelButtonLabel(pszLabel);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::SetNavigationRoot(IShellItem* psi)
{
	cout << "SetNavigationRoot" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialog2>(_systemDialog)->SetNavigationRoot(psi);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::HideControlsForHostedPickerProviderApp(void)
{
	cout << "HideControlsForHostedPickerProviderApp" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->HideControlsForHostedPickerProviderApp();
#endif
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::EnableControlsForHostedPickerProviderApp(void)
{
	cout << "EnableControlsForHostedPickerProviderApp" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->EnableControlsForHostedPickerProviderApp();
#endif
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::GetPrivateOptions(unsigned long* pfos)
{
	cout << "GetPrivateOptions" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->GetPrivateOptions(pfos);
#endif
	* pfos = 0;
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::SetPrivateOptions(unsigned long fos)
{
	cout << "SetPrivateOptions" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->SetPrivateOptions(fos);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::SetPersistenceKey(unsigned short const* pkey)
{
	cout << "SetPersistenceKey" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->SetPersistenceKey(pkey);
#endif
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::HasPlaces(void)
{
	cout << "HasPlaces" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->HasPlaces();
#endif
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::EnumPlaces(int plc, _GUID const& riid, void** ppv)
{
	cout << "EnumPlaces" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->EnumPlaces(plc, riid, ppv);
#endif
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::EnumControls(void** ppv)
{
	cout << "EnumControls" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->EnumControls(ppv);
#endif
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::GetPersistRegkey(unsigned short** preg)
{
	cout << "GetPersistRegkey" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->GetPersistRegkey(preg);
#endif
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::GetSavePropertyStore(IPropertyStore** ppstore, IPropertyDescriptionList** ppdesclist)
{
	cout << "GetSavePropertyStore" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->GetSavePropertyStore(ppstore, ppdesclist);
#endif
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::GetSaveExtension(unsigned short** pext)
{
	cout << "GetSaveExtension" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->GetSaveExtension(pext);
#endif
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::GetFileTypeControl(void** ftp)
{
	cout << "GetFileTypeControl" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->GetFileTypeControl(ftp);
#endif
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::GetFileNameControl(void** pctrl)
{
	cout << "GetFileNameControl" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->GetFileNameControl(pctrl);
#endif
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::GetFileProtectionControl(void** pfctrl)
{
	cout << "GetFileProtectionControl" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->GetFileProtectionControl(pfctrl);
#endif
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::SetFolderPrivate(IShellItem* psi, int arg)
{
	cout << "SetFolderPrivate" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->SetFolderPrivate(psi, arg);
#endif
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::SetCustomControlAreaHeight(unsigned int height)
{
	cout << "SetCustomControlAreaHeight" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->SetCustomControlAreaHeight(height);
#endif
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::GetDialogState(unsigned long arg, unsigned long* pstate)
{
	cout << "GetDialogState" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->GetDialogState(arg, pstate);
#endif
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::SetAppControlsModule(void* papp)
{
	cout << "SetAppControlsModule" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->SetAppControlsModule(papp);
#endif
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::SetUserEditedSaveProperties(void)
{
	cout << "SetUserEditedSaveProperties" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->SetUserEditedSaveProperties();
#endif
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::ShouldShowStandardNavigationRoots(void)
{
	cout << "ShouldShowStandardNavigationRoots" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->ShouldShowStandardNavigationRoots();
#endif
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::GetNavigationRoot(_GUID const& riid, void** ppv)
{
	cout << "GetNavigationRoot" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->GetNavigationRoot(riid, ppv);
#endif
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::ShouldShowFileProtectionControl(int* pfpc)
{
	cout << "ShouldShowFileProtectionControl" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->ShouldShowFileProtectionControl(pfpc);
#endif
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::GetCurrentDialogView(_GUID const& riid, void** ppv)
{
	cout << "GetCurrentDialogView" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->GetCurrentDialogView(riid, ppv);
#endif
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::SetSaveDialogEditBoxTextAndFileType(int arg, unsigned short const* pargb)
{
	cout << "SetSaveDialogEditBoxTextAndFileType" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->SetSaveDialogEditBoxTextAndFileType(arg, pargb);
#endif
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::MoveFocusFromBrowser(int arg)
{
	cout << "MoveFocusFromBrowser" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->MoveFocusFromBrowser(arg);
#endif
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::EnableOkButton(int enbl)
{
	cout << "EnableOkButton" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->EnableOkButton(enbl);
#endif
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::InitEnterpriseId(unsigned short const* pid)
{
	cout << "InitEnterpriseId" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->InitEnterpriseId(pid);
#endif
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::AdviseFirst(IFileDialogEvents* pfde, unsigned long* pdwCookie)
{
	cout << "AdviseFirst" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->AdviseFirst(pfde, pdwCookie);
#endif
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::HandleTab(void)
{
	cout << "HandleTab" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IFileDialogPrivate>(_systemDialog)->HandleTab();
#endif
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::SetSaveAsItem(IShellItem* psi)
{
	PWSTR pszPath = NULL;
	if (SUCCEEDED(psi->GetDisplayName(SIGDN_DESKTOPABSOLUTEPARSING, &pszPath)))
	{
		wcout << L"SetSaveAsItem, psi: " << pszPath << endl;
		CoTaskMemFree(pszPath);
	}
#ifdef SYSTEMDIALOG
	return _systemDialog->SetSaveAsItem(psi);
#endif
	if (_initFolder)
	{
		_initFolder->Release();
	}
	psi->GetParent(&_initFolder);
	if (SUCCEEDED(psi->GetDisplayName(SIGDN_NORMALDISPLAY, &pszPath)))
	{
		_initName = pszPath;
	}
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::SetProperties(IPropertyStore* pStore)
{
	cout << "SetProperties" << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->SetProperties(pStore);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::SetCollectedProperties(IPropertyDescriptionList* pList, BOOL fAppendDefault)
{
	cout << "SetCollectedProperties" << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->SetCollectedProperties(pList, fAppendDefault);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::GetProperties(IPropertyStore** ppStore)
{
	cout << "GetProperties" << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->GetProperties(ppStore);
#endif
	if (!_selectedItem.empty())
	{
		return SHGetPropertyStoreFromParsingName(_selectedItem.c_str(),
			NULL, GPS_DEFAULT, __uuidof(IPropertyStore), (void**)ppStore);
	}
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::ApplyProperties(IShellItem* psi, IPropertyStore* pStore, HWND hwnd, IFileOperationProgressSink* pSink)
{
	cout << "ApplyProperties" << endl;
#ifdef SYSTEMDIALOG
	return _systemDialog->ApplyProperties(psi, pStore, hwnd, pSink);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::GetWindow(HWND* phwnd)
{
	cout << "GetWindow" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IOleWindow>(_systemDialog)->GetWindow(phwnd);
#endif
	* phwnd = NULL;
	return S_OK;
}

HRESULT __stdcall CFilesSaveDialog::ContextSensitiveHelp(BOOL fEnterMode)
{
	cout << "ContextSensitiveHelp" << endl;
#ifdef SYSTEMDIALOG
	return AsInterface<IOleWindow>(_systemDialog)->ContextSensitiveHelp(fEnterMode);
#endif
	return S_OK;
}
