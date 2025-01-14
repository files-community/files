// Copyright (c) Files Community
// Licensed under the MIT License.

#include <iostream>
#include <algorithm>
#include <exdisp.h>
#include <iostream>
#include <objbase.h>
#include <propvarutil.h>
#include <shtypes.h>
#include <ShlObj_core.h>
#include <ShObjIdl_core.h>
#include <vector>
#include <wil/resource.h>

#include "OpenInFolder.h"

// Link additional libraries
#pragma comment(lib, "ole32.lib")
#pragma comment(lib, "Propsys.lib")
#pragma comment(lib, "user32.lib")
#pragma comment(lib, "uuid.lib")

constexpr auto ID_TIMEREXPIRED = 101;

LRESULT CALLBACK WindowProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam);
bool OpenInExistingShellWindow(const TCHAR* folderPath);
void RunFileExplorer(const TCHAR* openDirectory);
size_t strifind(const std::wstring& strHaystack, const std::wstring& strNeedle);
bool comparei(std::wstring stringA, std::wstring stringB);
std::string wstring_to_utf8_hex(const std::wstring& input);
std::wstring str2wstr(const std::string& str);

int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int cmdShow)
{
	auto oleCleanup = wil::OleInitialize_failfast();

	FILE* _debugStream = NULL;
	PWSTR pszPath = NULL;
	HRESULT hr = SHGetKnownFolderPath(FOLDERID_Desktop, 0, NULL, &pszPath);

	if (SUCCEEDED(hr))
	{
		TCHAR debugPath[MAX_PATH];
		swprintf(debugPath, _countof(debugPath) - 1, L"%s\\%s", pszPath, L"open_in_folder.txt");
		//_wfreopen_s(&_debugStream, debugPath, L"w", stdout);
		CoTaskMemFree(pszPath);
	}

	//Uncomment to attach debugger
	//Sleep(10 * 1000);

	int numArgs = 0;
	bool withArgs = false;
	LPWSTR* szArglist = CommandLineToArgvW(GetCommandLine(), &numArgs);
	WCHAR openDirectory[MAX_PATH];

	if (numArgs > 1 && wcsnlen(szArglist[1], 1))
	{
		swprintf(openDirectory, _countof(openDirectory) - 1, L"%s", szArglist[1]);
		std::wcout << openDirectory << std::endl;
		withArgs = true;
	}

	LocalFree(szArglist);

	WCHAR szBuf[MAX_PATH];
	ExpandEnvironmentStringsW(L"%LOCALAPPDATA%\\Microsoft\\WindowsApps\\files-dev.exe", szBuf, MAX_PATH - 1);
	std::wcout << szBuf << std::endl;
	if (_waccess(szBuf, 0) == -1)
	{
		std::cout << "Files has been uninstalled" << std::endl;

		MessageBox(
			NULL,
			(LPCWSTR)L"Files has been uninstalled. Restoring File Explorer.",
			(LPCWSTR)L"Files",
			(UINT)(MB_OK)
		);

		// Uninstall launcher
		TCHAR szCmd[MAX_PATH];
		swprintf(szCmd, _countof(szCmd) - 1, L"/c reg.exe import \"%s\"", L"%LocalAppData%\\Files\\UnsetFilesAsDefault.reg");
		if (((int)ShellExecute(0, L"runas", L"cmd.exe", szCmd, 0, SW_HIDE) > 32))
		{
			std::cout << "Launcher unset as default" << std::endl;
			swprintf(szCmd, _countof(szCmd) - 1, L"-command \"Start-Sleep -Seconds 5; $lfp = [System.Environment]::ExpandEnvironmentVariables('%%LocalAppData%%\\Files'); Remove-Item -Path $lfp -Recurse -Force\"");
			if ((int)ShellExecute(0, 0, L"powershell.exe", szCmd, 0, SW_HIDE) > 32)
			{
				std::cout << "Launcher uninstalled" << std::endl;
			}
		}

		// Run explorer
		RunFileExplorer(withArgs ? openDirectory : NULL);

		if (_debugStream)
			fclose(_debugStream);

		return 0;
	}

	if (withArgs)
	{
		if (OpenInExistingShellWindow(openDirectory))
		{
			if (_debugStream)
				fclose(_debugStream);

			return 0;
		}

		// Register the window class.
		const wchar_t CLASS_NAME[] = L"Files Window Class";

		WNDCLASSEX wcex = { };
		wcex.cbSize = sizeof(wcex);
		wcex.lpfnWndProc = WindowProc;
		wcex.cbWndExtra = sizeof(OpenInFolder*);
		wcex.hInstance = hInstance;
		wcex.lpszClassName = CLASS_NAME;
		RegisterClassEx(&wcex);

		OpenInFolder openInFolder;

		// Create the window.
		HWND hwnd = CreateWindowEx(
			0,
			CLASS_NAME,
			L"Files Launcher",
			0,
			0, 0, 0, 0,
			HWND_MESSAGE,
			NULL,
			hInstance,
			&openInFolder
		);

		if (hwnd == NULL)
		{
			if (_debugStream)
				fclose(_debugStream);

			return 0;
		}

		SetTimer(hwnd, ID_TIMEREXPIRED, 500, NULL);

		ShowWindow(hwnd, SW_SHOWNORMAL);
		//UpdateWindow(hwnd);

		MSG msg = { };
		while (GetMessage(&msg, NULL, 0, 0) > 0)
		{
			TranslateMessage(&msg);
			DispatchMessage(&msg);
		}

		auto item = openInFolder.GetResult();

		TCHAR args[1024];
		if (item.empty())
		{
			std::wcout << L"No item selected" << std::endl;
			//swprintf(args, _countof(args) - 1, L"-directory \"%s\"", openDirectory);
			swprintf(args, _countof(args) - 1, L"\"%s\" -directory \"%s\"", szBuf, openDirectory);
		}
		else
		{
			std::wcout << L"Item: " << item << std::endl;
			//swprintf(args, _countof(args) - 1, L"-select \"%s\"", item.c_str());
			swprintf(args, _countof(args) - 1, L"\"%s\" -select \"%s\"", szBuf, item.c_str());
		}

		std::wstring uriWithArgs = L"files-dev:?cmd=" + str2wstr(wstring_to_utf8_hex(args));

		std::wcout << L"Invoking: " << args << L" = " << uriWithArgs << std::endl;

		SHELLEXECUTEINFO ShExecInfo = { 0 };
		ShExecInfo.cbSize = sizeof(SHELLEXECUTEINFO);
		ShExecInfo.fMask = SEE_MASK_NOASYNC | SEE_MASK_FLAG_NO_UI;
		ShExecInfo.lpFile = uriWithArgs.c_str();
		ShExecInfo.lpDirectory = openDirectory;
		ShExecInfo.nShow = SW_SHOW;

		if (!ShellExecuteEx(&ShExecInfo))
		{
			std::wcout << L"Protocol error: " << GetLastError() << std::endl;

			//ShExecInfo.lpFile = L"files-dev.exe";
			//ShExecInfo.lpParameters = args;
			//if (!ShellExecuteEx(&ShExecInfo))
			//{
				//std::wcout << L"Command line error: " << GetLastError() << std::endl;
			//}
		}
	}
	else
	{
		std::wcout << L"Invoking: no arguments" << std::endl;

		SHELLEXECUTEINFO ShExecInfo = { 0 };
		ShExecInfo.cbSize = sizeof(SHELLEXECUTEINFO);
		ShExecInfo.fMask = SEE_MASK_NOASYNC | SEE_MASK_FLAG_NO_UI;
		ShExecInfo.lpFile = L"files-dev:";
		ShExecInfo.nShow = SW_SHOW;

		if (!ShellExecuteEx(&ShExecInfo))
		{
			std::wcout << L"Protocol error: " << GetLastError() << std::endl;
			//ShExecInfo.lpFile = L"files-dev.exe";
			//if (!ShellExecuteEx(&ShExecInfo))
			//{
				//std::wcout << L"Command line error: " << GetLastError() << std::endl;
			//}
		}
	}

	if (_debugStream)
		fclose(_debugStream);

	return 0;
}

LRESULT CALLBACK WindowProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	auto* pContainer = (OpenInFolder*)GetWindowLongPtr(hwnd, GWLP_USERDATA);

	switch (uMsg)
	{
	case WM_NCCREATE:
	{
		CREATESTRUCT* pCreate = reinterpret_cast<CREATESTRUCT*>(lParam);
		pContainer = reinterpret_cast<OpenInFolder*>(pCreate->lpCreateParams);

		if (!pContainer)
		{
			PostQuitMessage(0);
			return 0;
		}

		pContainer->SetWindow(hwnd);
		SetWindowLongPtr(hwnd, GWLP_USERDATA, (LONG_PTR)pContainer);
		break;
	}

	case WM_NCDESTROY:
		SetWindowLongPtr(hwnd, GWLP_USERDATA, 0);
		return 0;

	case WM_TIMER:
		switch (wParam)
		{
		case ID_TIMEREXPIRED:
			PostQuitMessage(0);
			return 0;
		}
		break;
	}

	 // Jump across to the member window function (will handle all requests).
	if (pContainer != nullptr)
		return pContainer->WindowProcedure(hwnd, uMsg, wParam, lParam);
	else
		return DefWindowProc(hwnd, uMsg, wParam, lParam);
}

size_t strifind(const std::wstring& strHaystack, const std::wstring& strNeedle)
{
	auto it = std::search(
		strHaystack.begin(), strHaystack.end(),
		strNeedle.begin(), strNeedle.end(),
		[](wchar_t ch1, wchar_t ch2) { return std::toupper(ch1) == std::toupper(ch2); }
	);

	return it != strHaystack.end() ? it - strHaystack.begin() : std::wstring::npos;
}

bool comparei(std::wstring stringA, std::wstring stringB)
{
	transform(stringA.begin(), stringA.end(), stringA.begin(), std::toupper);
	transform(stringB.begin(), stringB.end(), stringB.begin(), std::toupper);

	return (stringA == stringB);
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

void RunFileExplorer(const TCHAR* openDirectory)
{
	// Run explorer
	SHELLEXECUTEINFO ShExecInfo = { 0 };
	ShExecInfo.cbSize = sizeof(SHELLEXECUTEINFO);
	ShExecInfo.lpFile = L"explorer.exe";

	if (openDirectory != NULL)
	{
		TCHAR args[1024];
		swprintf(args, _countof(args) - 1, L"\"%s\"", openDirectory);
		ShExecInfo.lpParameters = args;
	}

	ShExecInfo.nShow = SW_SHOW;
	ShellExecuteEx(&ShExecInfo);
}

bool OpenInExistingShellWindow(const TCHAR* folderPath)
{
	std::wstring openDirectory(folderPath);
	bool mustOpenInExplorer = false;

	if (strifind(openDirectory, L"::{") == 0)
		openDirectory = L"shell:" + openDirectory;

	if (strifind(openDirectory, L"shell:") == 0)
	{
		std::vector<std::wstring> supportedShellFolders{
			L"shell:::{645FF040-5081-101B-9F08-00AA002F954E}",
			L"shell:::{5E5F29CE-E0A8-49D3-AF32-7A7BDC173478}",
			L"shell:::{20D04FE0-3AEA-1069-A2D8-08002B30309D}",
			L"shell:::{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}",
			L"shell:::{208D2C60-3AEA-1069-A2D7-08002B30309D}",
			L"Shell:RecycleBinFolder", L"Shell:NetworkPlacesFolder", L"Shell:MyComputerFolder"
		};

		auto it = std::find_if(
			supportedShellFolders.begin(), supportedShellFolders.end(),
			[openDirectory](std::wstring it) { return comparei(it, openDirectory); }
		);

		mustOpenInExplorer = it == supportedShellFolders.end();
	}

	IShellItem* psi;
	PIDLIST_ABSOLUTE controlPanelCategoryViewPidl;

	if (!SUCCEEDED(SHCreateItemFromParsingName(L"::{26EE0668-A00A-44D7-9371-BEB064C98683}", NULL, IID_IShellItem, (void**)&psi)))
	{
		if (mustOpenInExplorer)
			RunFileExplorer(openDirectory.c_str());

		return mustOpenInExplorer;
	}

	if (!SUCCEEDED(SHGetIDListFromObject(psi, &controlPanelCategoryViewPidl)))
	{
		psi->Release();
		if (mustOpenInExplorer)
			RunFileExplorer(openDirectory.c_str());

		return mustOpenInExplorer;
	}

	psi->Release();

	PIDLIST_ABSOLUTE targetFolderPidl;

	if (!SUCCEEDED(SHCreateItemFromParsingName(openDirectory.c_str(), NULL, IID_IShellItem, (void**)&psi)))
	{
		if (mustOpenInExplorer)
			RunFileExplorer(openDirectory.c_str());

		return mustOpenInExplorer;
	}

	if (!SUCCEEDED(SHGetIDListFromObject(psi, &targetFolderPidl)))
	{
		psi->Release();
		if (mustOpenInExplorer)
			RunFileExplorer(openDirectory.c_str());

		return mustOpenInExplorer;
	}

	psi->Release();

	bool opened = false;
	IShellWindows* shellWindows;
	if (SUCCEEDED(CoCreateInstance(CLSID_ShellWindows, NULL, CLSCTX_LOCAL_SERVER, IID_IShellWindows, (void**)&shellWindows)))
	{
		VARIANT v;
		V_VT(&v) = VT_I4;
		IDispatch* item;
		for (V_I4(&v) = 0; SUCCEEDED(shellWindows->Item(v, &item)) && item != NULL; V_I4(&v)++)
		{
			IServiceProvider* serv;
			if (SUCCEEDED(item->QueryInterface(IID_IServiceProvider, (void**)&serv)))
			{
				IShellBrowser* shellBrowser;
				if (SUCCEEDED(serv->QueryService(SID_STopLevelBrowser, IID_IShellBrowser, (void**)&shellBrowser)))
				{
					IShellView* shellView;
					if (SUCCEEDED(shellBrowser->QueryActiveShellView(&shellView)))
					{
						IFolderView* folderView;
						if (SUCCEEDED(shellView->QueryInterface(IID_IFolderView, (void**)&folderView)))
						{
							IPersistFolder2* folder;
							if (SUCCEEDED(folderView->GetFolder(IID_IPersistFolder2, (void**)&folder)))
							{
								PIDLIST_ABSOLUTE folderPidl;
								if (SUCCEEDED(folder->GetCurFolder(&folderPidl)))
								{
									if (ILIsParent(folderPidl, targetFolderPidl, true) ||
										ILIsEqual(folderPidl, controlPanelCategoryViewPidl))
									{
										if (SUCCEEDED(shellBrowser->BrowseObject(targetFolderPidl, SBSP_SAMEBROWSER | SBSP_ABSOLUTE)))
										{
											opened = true;
											break;
										}
									}
									CoTaskMemFree(folderPidl);
								}
								folder->Release();
							}
							folderView->Release();
						}
						shellView->Release();
					}
					shellBrowser->Release();
				}
				serv->Release();
			}
			item->Release();
		}

		shellWindows->Release();
	}

	CoTaskMemFree(targetFolderPidl);
	CoTaskMemFree(controlPanelCategoryViewPidl);

	if (!opened && mustOpenInExplorer)
		RunFileExplorer(openDirectory.c_str());

	return opened || mustOpenInExplorer;
}
