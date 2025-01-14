// Copyright (c) Files Community
// Licensed under the MIT License.

// Abstract:
//  Declaration of CFilesOpenDialog.

#pragma once

#define DEBUGLOG

#include <iostream>
#include <string>
#include <vector>

// Main symbols
#include "resource.h"
#include "CustomOpenDialog_i.h"
#include "UndefInterfaces.h"

#if defined(_WIN32_WCE) && !defined(_CE_DCOM) && !defined(_CE_ALLOW_SINGLE_THREADED_OBJECTS_IN_MTA)
#error "Single-threaded COM objects are not supported properly on the Windows CE platform, for example Windows Mobile platforms do not include full DCOM support. Define _CE_ALLOW_SINGLE_THREADED_OBJECTS_IN_MTA to make ATL support the creation of single-threaded COM objects and allow implementations with single-threaded COM objects. The threading model in the RGS file has been set to 'Free' as it is the only threading model supported on non-DCOM Windows CE platforms."
#endif

#define STDAPICALL HRESULT __stdcall

using namespace ATL;

class ATL_NO_VTABLE CFilesOpenDialog :
	public CComObjectRootEx<CComSingleThreadModel>,
	public CComCoClass<CFilesOpenDialog, &CLSID_FilesOpenDialog>,
	public IFileDialog,
	public IFileDialog2,
	public IFileOpenDialog,
	public IFileDialogCustomize,
	public IObjectWithSite,
	public IFileDialogPrivate
{
public:
	CFilesOpenDialog();

DECLARE_REGISTRY_RESOURCEID(106)

CUSTOM_BEGIN_COM_MAP(CFilesOpenDialog)
	COM_INTERFACE_ENTRY(IFileDialog)
	COM_INTERFACE_ENTRY(IFileDialog2)
	COM_INTERFACE_ENTRY(IFileOpenDialog)
	COM_INTERFACE_ENTRY(IFileDialogCustomize)
	COM_INTERFACE_ENTRY(IObjectWithSite)
	COM_INTERFACE_ENTRY(IFileDialogPrivate)
END_COM_MAP()

	DECLARE_PROTECT_FINAL_CONSTRUCT()

	HRESULT FinalConstruct()
	{
		return S_OK;
	}

	void FinalRelease();

	CComPtr<IFileOpenDialog> _systemDialog;

	FILEOPENDIALOGOPTIONS _fos;

	std::vector<std::wstring> _selectedItems;
	std::wstring _outputPath;
	IShellItem* _initFolder;
	IFileDialogEvents* _dialogEvents;

	FILE* _debugStream;

public:
	// Inherited through IFileOpenDialog
	STDAPICALL Show(HWND hwndOwner) override;
	STDAPICALL SetFileTypes(UINT cFileTypes, const COMDLG_FILTERSPEC* rgFilterSpec) override;
	STDAPICALL SetFileTypeIndex(UINT iFileType) override;
	STDAPICALL GetFileTypeIndex(UINT* piFileType) override;
	STDAPICALL Advise(IFileDialogEvents* pfde, DWORD* pdwCookie) override;
	STDAPICALL Unadvise(DWORD dwCookie) override;
	STDAPICALL SetOptions(FILEOPENDIALOGOPTIONS fos) override;
	STDAPICALL GetOptions(FILEOPENDIALOGOPTIONS* pfos) override;
	STDAPICALL SetDefaultFolder(IShellItem* psi) override;
	STDAPICALL SetFolder(IShellItem* psi) override;
	STDAPICALL GetFolder(IShellItem** ppsi) override;
	STDAPICALL GetCurrentSelection(IShellItem** ppsi) override;
	STDAPICALL SetFileName(LPCWSTR pszName) override;
	STDAPICALL GetFileName(LPWSTR* pszName) override;
	STDAPICALL SetTitle(LPCWSTR pszTitle) override;
	STDAPICALL SetOkButtonLabel(LPCWSTR pszText) override;
	STDAPICALL SetFileNameLabel(LPCWSTR pszLabel) override;
	STDAPICALL GetResult(IShellItem** ppsi) override;
	STDAPICALL AddPlace(IShellItem* psi, FDAP fdap) override;
	STDAPICALL SetDefaultExtension(LPCWSTR pszDefaultExtension) override;
	STDAPICALL Close(HRESULT hr) override;
	STDAPICALL SetClientGuid(REFGUID guid) override;
	STDAPICALL ClearClientData(void) override;
	STDAPICALL SetFilter(IShellItemFilter* pFilter) override;
	STDAPICALL GetResults(IShellItemArray** ppenum) override;
	STDAPICALL GetSelectedItems(IShellItemArray** ppsai) override;

	// Inherited through IFileDialogCustomize
	STDAPICALL EnableOpenDropDown(DWORD dwIDCtl) override;
	STDAPICALL AddMenu(DWORD dwIDCtl, LPCWSTR pszLabel) override;
	STDAPICALL AddPushButton(DWORD dwIDCtl, LPCWSTR pszLabel) override;
	STDAPICALL AddComboBox(DWORD dwIDCtl) override;
	STDAPICALL AddRadioButtonList(DWORD dwIDCtl) override;
	STDAPICALL AddCheckButton(DWORD dwIDCtl, LPCWSTR pszLabel, BOOL bChecked) override;
	STDAPICALL AddEditBox(DWORD dwIDCtl, LPCWSTR pszText) override;
	STDAPICALL AddSeparator(DWORD dwIDCtl) override;
	STDAPICALL AddText(DWORD dwIDCtl, LPCWSTR pszText) override;
	STDAPICALL SetControlLabel(DWORD dwIDCtl, LPCWSTR pszLabel) override;
	STDAPICALL GetControlState(DWORD dwIDCtl, CDCONTROLSTATEF* pdwState) override;
	STDAPICALL SetControlState(DWORD dwIDCtl, CDCONTROLSTATEF dwState) override;
	STDAPICALL GetEditBoxText(DWORD dwIDCtl, WCHAR** ppszText) override;
	STDAPICALL SetEditBoxText(DWORD dwIDCtl, LPCWSTR pszText) override;
	STDAPICALL GetCheckButtonState(DWORD dwIDCtl, BOOL* pbChecked) override;
	STDAPICALL SetCheckButtonState(DWORD dwIDCtl, BOOL bChecked) override;
	STDAPICALL AddControlItem(DWORD dwIDCtl, DWORD dwIDItem, LPCWSTR pszLabel) override;
	STDAPICALL RemoveControlItem(DWORD dwIDCtl, DWORD dwIDItem) override;
	STDAPICALL RemoveAllControlItems(DWORD dwIDCtl) override;
	STDAPICALL GetControlItemState(DWORD dwIDCtl, DWORD dwIDItem, CDCONTROLSTATEF* pdwState) override;
	STDAPICALL SetControlItemState(DWORD dwIDCtl, DWORD dwIDItem, CDCONTROLSTATEF dwState) override;
	STDAPICALL GetSelectedControlItem(DWORD dwIDCtl, DWORD* pdwIDItem) override;
	STDAPICALL SetSelectedControlItem(DWORD dwIDCtl, DWORD dwIDItem) override;
	STDAPICALL StartVisualGroup(DWORD dwIDCtl, LPCWSTR pszLabel) override;
	STDAPICALL EndVisualGroup(void) override;
	STDAPICALL MakeProminent(DWORD dwIDCtl) override;
	STDAPICALL SetControlItemText(DWORD dwIDCtl, DWORD dwIDItem, LPCWSTR pszLabel) override;

	// Inherited through IFileDialog2
	STDAPICALL SetCancelButtonLabel(LPCWSTR pszLabel) override;
	STDAPICALL SetNavigationRoot(IShellItem* psi) override;

	// Inherited through IObjectWithSite
	STDAPICALL SetSite(IUnknown* pUnkSite) override;
	STDAPICALL GetSite(REFIID riid, void** ppvSite) override;

	// Inherited through IFileDialogPrivate
	STDAPICALL HideControlsForHostedPickerProviderApp(void) override;
	STDAPICALL EnableControlsForHostedPickerProviderApp(void) override;
	STDAPICALL GetPrivateOptions(unsigned long*) override;
	STDAPICALL SetPrivateOptions(unsigned long) override;
	STDAPICALL SetPersistenceKey(unsigned short const*) override;
	STDAPICALL HasPlaces(void) override;
	STDAPICALL EnumPlaces(int, _GUID const&, void**) override;
	STDAPICALL EnumControls(void**) override;
	STDAPICALL GetPersistRegkey(unsigned short**) override;
	STDAPICALL GetSavePropertyStore(IPropertyStore**, IPropertyDescriptionList**) override;
	STDAPICALL GetSaveExtension(unsigned short**) override;
	STDAPICALL GetFileTypeControl(void**) override;
	STDAPICALL GetFileNameControl(void**) override;
	STDAPICALL GetFileProtectionControl(void**) override;
	STDAPICALL SetFolderPrivate(IShellItem*, int) override;
	STDAPICALL SetCustomControlAreaHeight(unsigned int) override;
	STDAPICALL GetDialogState(unsigned long, unsigned long*) override;
	STDAPICALL SetAppControlsModule(void*) override;
	STDAPICALL SetUserEditedSaveProperties(void) override;
	STDAPICALL ShouldShowStandardNavigationRoots(void) override;
	STDAPICALL GetNavigationRoot(_GUID const&, void**) override;
	STDAPICALL ShouldShowFileProtectionControl(int*) override;
	STDAPICALL GetCurrentDialogView(_GUID const&, void**) override;
	STDAPICALL SetSaveDialogEditBoxTextAndFileType(int, unsigned short const*) override;
	STDAPICALL MoveFocusFromBrowser(int) override;
	STDAPICALL EnableOkButton(int) override;
	STDAPICALL InitEnterpriseId(unsigned short const*) override;
	STDAPICALL AdviseFirst(IFileDialogEvents*, unsigned long*) override;
	STDAPICALL HandleTab(void) override;
};

OBJECT_ENTRY_AUTO(__uuidof(FilesOpenDialog), CFilesOpenDialog)
