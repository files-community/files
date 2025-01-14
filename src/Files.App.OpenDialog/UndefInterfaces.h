// Copyright (c) Files Community
// Licensed under the MIT License.

// Abstract:
//  Declaration of Undocumented interfaces and helpers.

#pragma once

#include "framework.h"
#include "shobjidl.h"

using namespace ATL;

#ifdef DEBUGLOG

#define CUSTOM_BEGIN_COM_MAP(x) public: \
	typedef x _ComMapClass; \
	static HRESULT WINAPI _Cache(_In_ void* pv, _In_ REFIID iid, _COM_Outptr_result_maybenull_ void** ppvObject, _In_ DWORD_PTR dw) throw() \
	{ \
		_ComMapClass* p = (_ComMapClass*)pv;\
		p->Lock();\
		HRESULT hRes = E_FAIL; \
		__try \
		{ \
			hRes = ATL::CComObjectRootBase::_Cache(pv, iid, ppvObject, dw);\
		} \
		__finally \
		{ \
			p->Unlock();\
		} \
		return hRes;\
	} \
	IUnknown* _GetRawUnknown() throw() \
	{ ATLASSERT(_GetEntries()[0].pFunc == _ATL_SIMPLEMAPENTRY); return (IUnknown*)((INT_PTR)this+_GetEntries()->dw); } \
	_ATL_DECLARE_GET_UNKNOWN(x) \
	HRESULT _InternalQueryInterface( \
		_In_ REFIID iid, \
		_COM_Outptr_ void** ppvObject) throw() \
	{ \
		HRESULT res = this->InternalQueryInterface(this, _GetEntries(), iid, ppvObject); \
		OLECHAR* guidString; \
		(void)StringFromCLSID(iid, &guidString); \
		std::wcout << L"QueryInterface: " << guidString << L" = " << res << std::endl; \
		::CoTaskMemFree(guidString); \
		return res; \
	} \
	const static ATL::_ATL_INTMAP_ENTRY* WINAPI _GetEntries() throw() { \
	static const ATL::_ATL_INTMAP_ENTRY _entries[] = { DEBUG_QI_ENTRY(x)

#else

#define CUSTOM_BEGIN_COM_MAP(x) BEGIN_COM_MAP(x)

#endif // DEBUGLOG


MIDL_INTERFACE("9EA5491C-89C8-4BEF-93D3-7F665FB82A33")
IFileDialogPrivate : public IUnknown
{
public:
	virtual HRESULT STDMETHODCALLTYPE HideControlsForHostedPickerProviderApp(void) = 0;
	virtual HRESULT STDMETHODCALLTYPE EnableControlsForHostedPickerProviderApp(void) = 0;
	virtual HRESULT STDMETHODCALLTYPE GetPrivateOptions(unsigned long*) = 0;
	virtual HRESULT STDMETHODCALLTYPE SetPrivateOptions(unsigned long) = 0;
	virtual HRESULT STDMETHODCALLTYPE SetPersistenceKey(unsigned short const*) = 0;
	virtual HRESULT STDMETHODCALLTYPE HasPlaces(void) = 0;
	virtual HRESULT STDMETHODCALLTYPE EnumPlaces(int, _GUID const&, void**) = 0; //tagFDPEPLACES
	virtual HRESULT STDMETHODCALLTYPE EnumControls(void**) = 0; //IEnumAppControl
	virtual HRESULT STDMETHODCALLTYPE GetPersistRegkey(unsigned short**) = 0;
	virtual HRESULT STDMETHODCALLTYPE GetSavePropertyStore(IPropertyStore**, IPropertyDescriptionList**) = 0;
	virtual HRESULT STDMETHODCALLTYPE GetSaveExtension(unsigned short**) = 0;
	virtual HRESULT STDMETHODCALLTYPE GetFileTypeControl(void**) = 0; //IAppControl
	virtual HRESULT STDMETHODCALLTYPE GetFileNameControl(void**) = 0; //IAppControl
	virtual HRESULT STDMETHODCALLTYPE GetFileProtectionControl(void**) = 0;// IAppControl
	virtual HRESULT STDMETHODCALLTYPE SetFolderPrivate(IShellItem*, int) = 0;
	virtual HRESULT STDMETHODCALLTYPE SetCustomControlAreaHeight(unsigned int) = 0;
	virtual HRESULT STDMETHODCALLTYPE GetDialogState(unsigned long, unsigned long*) = 0;
	virtual HRESULT STDMETHODCALLTYPE SetAppControlsModule(void*) = 0;// IAppControlsModule
	virtual HRESULT STDMETHODCALLTYPE SetUserEditedSaveProperties(void) = 0;
	virtual HRESULT STDMETHODCALLTYPE ShouldShowStandardNavigationRoots(void) = 0;
	virtual HRESULT STDMETHODCALLTYPE GetNavigationRoot(_GUID const&, void**) = 0;
	virtual HRESULT STDMETHODCALLTYPE ShouldShowFileProtectionControl(int*) = 0;
	virtual HRESULT STDMETHODCALLTYPE GetCurrentDialogView(_GUID const&, void**) = 0;
	virtual HRESULT STDMETHODCALLTYPE SetSaveDialogEditBoxTextAndFileType(int, unsigned short const*) = 0;
	virtual HRESULT STDMETHODCALLTYPE MoveFocusFromBrowser(int) = 0;
	virtual HRESULT STDMETHODCALLTYPE EnableOkButton(int) = 0;
	virtual HRESULT STDMETHODCALLTYPE InitEnterpriseId(unsigned short const*) = 0;
	virtual HRESULT STDMETHODCALLTYPE AdviseFirst(IFileDialogEvents*, unsigned long*) = 0;
	virtual HRESULT STDMETHODCALLTYPE HandleTab(void) = 0;
};
