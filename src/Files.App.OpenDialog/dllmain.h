// Copyright (c) Files Community
// Licensed under the MIT License.

// Abstract:
//  declaration of module class.

class CCustomOpenDialogModule :
	public ATL::CAtlDllModuleT<CCustomOpenDialogModule>
{
public :
	DECLARE_LIBID(LIBID_CustomOpenDialogLib)
	DECLARE_REGISTRY_APPID_RESOURCEID(IDR_CUSTOMOPENDIALOG, "{14533360-0b0c-44e6-9e5f-8ec8a478158d}")
};

extern class CCustomOpenDialogModule _AtlModule;
