// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.ViewModels.Properties;
using Microsoft.UI.Content;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using System.Runtime.InteropServices;
using System.Text;
using Vanara.PInvoke;
using Windows.Win32;
using Windows.Win32.System.Com;
using Windows.Win32.Graphics.Direct3D;
using Windows.Win32.Graphics.Direct3D11;
using Windows.Win32.Graphics.DirectComposition;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.Graphics.Dxgi;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT;
using static Vanara.PInvoke.ShlwApi;
using static Vanara.PInvoke.User32;

#pragma warning disable CS8305 // Type is for evaluation purposes only and is subject to change or removal in future updates.

namespace Files.App.ViewModels.Previews
{
	public sealed class ShellPreviewViewModel : BasePreviewModel
	{
		private const string IPreviewHandlerIid = "{8895b1c6-b41f-4c1c-a562-0d564250836f}";
		private static readonly Guid QueryAssociationsClsid = new Guid(0xa07034fd, 0x6caa, 0x4954, 0xac, 0x3f, 0x97, 0xa2, 0x72, 0x16, 0xf9, 0x8a);
		private static readonly Guid IQueryAssociationsIid = Guid.ParseExact("c46ca590-3c3f-11d2-bee6-0000f805ca57", "d");

		PreviewHandler? currentHandler;
		ContentExternalOutputLink? outputLink;
		WindowClass? wCls;
		HWND hwnd = HWND.NULL;
		bool isOfficePreview = false;

		public ShellPreviewViewModel(ListedItem item) : base(item)
		{
		}

		public async override Task<List<FileProperty>> LoadPreviewAndDetailsAsync()
			=> [];

		public static Guid? FindPreviewHandlerFor(string extension, nint hwnd)
		{
			if (string.IsNullOrEmpty(extension))
				return null;

			var hr = AssocCreate(QueryAssociationsClsid, IQueryAssociationsIid, out var queryAssoc);
			if (!hr.Succeeded)
				return null;

			try
			{
				if (queryAssoc == null)
					return null;

				queryAssoc.Init(ASSOCF.ASSOCF_INIT_DEFAULTTOSTAR, extension, nint.Zero, hwnd);

				var sb = new StringBuilder(128);
				uint cch = 64;

				queryAssoc.GetString(ASSOCF.ASSOCF_NOTRUNCATE, ASSOCSTR.ASSOCSTR_SHELLEXTENSION, IPreviewHandlerIid, sb, ref cch);

				Debug.WriteLine($"Preview handler for {extension}: {sb}");
				return Guid.Parse(sb.ToString());
			}
			catch
			{
				return null;
			}
			finally
			{
				Marshal.ReleaseComObject(queryAssoc);
			}
		}

		public void SizeChanged(Windows.Foundation.Rect size)
		{
			var width = (int)size.Width;
			var height = (int)size.Height;

			if (hwnd != HWND.NULL)
				SetWindowPos(hwnd, HWND.HWND_TOP, (int)size.Left, (int)size.Top, width, height, SetWindowPosFlags.SWP_NOACTIVATE);

			currentHandler?.ResetBounds(new(0, 0, width, height));

			if (outputLink is not null)
				outputLink.PlacementVisual.Size = new(width, height);
		}

		private nint WndProc(HWND hwnd, uint msg, nint wParam, nint lParam)
		{
			if (msg == (uint)WindowMessage.WM_CREATE)
			{
				var clsid = FindPreviewHandlerFor(Item.FileExtension, hwnd.DangerousGetHandle());

				isOfficePreview = new Guid?[]
				{
					Guid.Parse("84F66100-FF7C-4fb4-B0C0-02CD7FB668FE"), // preview handler for Word files
					Guid.Parse("65235197-874B-4A07-BDC5-E65EA825B718"), // preview handler for PowerPoint files
					Guid.Parse("00020827-0000-0000-C000-000000000046")  // preview handler for Excel files
				}.Contains(clsid);

				try
				{
					currentHandler = new PreviewHandler(clsid.Value, hwnd.DangerousGetHandle());
					currentHandler.InitWithFileWithEveryWay(Item.ItemPath);
					currentHandler.DoPreview();
				}
				catch
				{
					UnloadPreview();
				}
			}
			else if (msg == (uint)WindowMessage.WM_DESTROY)
			{
				if (currentHandler is not null)
				{
					currentHandler.UnloadPreview();
					currentHandler = null;
				}
			}

			return DefWindowProc(hwnd, msg, wParam, lParam);
		}

		public void LoadPreview(UIElement presenter)
		{
			var parent = MainWindow.Instance.WindowHandle;

			HINSTANCE hInst = Kernel32.GetModuleHandle();

			wCls = new WindowClass($"{GetType().Name}{Guid.NewGuid()}", hInst, WndProc);

			hwnd = CreateWindowEx(
				WindowStylesEx.WS_EX_LAYERED | WindowStylesEx.WS_EX_COMPOSITED,
				wCls.ClassName,
				"Preview",
				WindowStyles.WS_CHILD | WindowStyles.WS_CLIPSIBLINGS | WindowStyles.WS_VISIBLE,
				0, 0, 0, 0,
				hWndParent: parent,
				hInstance: hInst);

			_ = ChildWindowToXaml(parent, presenter);
		}

		private unsafe bool ChildWindowToXaml(nint parent, UIElement presenter)
		{
			D3D_DRIVER_TYPE[] driverTypes =
			[
				D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_HARDWARE,
				D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_WARP,
			];

			ID3D11Device* pD3D11Device = default;
			ID3D11DeviceContext* pD3D11DeviceContext = default;

			foreach (var driveType in driverTypes)
			{
				var hr = PInvoke.D3D11CreateDevice(
					null,
					driveType,
					new(nint.Zero),
					D3D11_CREATE_DEVICE_FLAG.D3D11_CREATE_DEVICE_BGRA_SUPPORT,
					null,
					0,
					7,
					&pD3D11Device,
					null,
					&pD3D11DeviceContext);

				if (hr.Succeeded)
					break;
			}

			if (pD3D11Device is null)
				return false;

			IDXGIDevice* pDXGIDevice = (IDXGIDevice*)pD3D11Device;
			if (PInvoke.DCompositionCreateDevice(pDXGIDevice, typeof(IDCompositionDevice).GUID, out var compositionDevicePtr).Failed)
				return false;

			var pDCompositionDevice = (IDCompositionDevice*)compositionDevicePtr;
			IDCompositionVisual* pChildVisual = default;
			IUnknown* pControlSurface = default;

			pDCompositionDevice->CreateVisual(&pChildVisual);
			pDCompositionDevice->CreateSurfaceFromHwnd(new(hwnd.DangerousGetHandle()), &pControlSurface);
			pChildVisual->SetContent(pControlSurface);
			if (pChildVisual is null || pControlSurface is null)
				return false;

			var compositor = ElementCompositionPreview.GetElementVisual(presenter).Compositor;
			outputLink = ContentExternalOutputLink.Create(compositor);

			var target = outputLink.As<IDCompositionTarget.Interface>();
			target.SetRoot(pChildVisual);

			outputLink.PlacementVisual.Size = new(0, 0);
			outputLink.PlacementVisual.Scale = new(1/(float)presenter.XamlRoot.RasterizationScale);
			ElementCompositionPreview.SetElementChildVisual(presenter, outputLink.PlacementVisual);

			pDCompositionDevice->Commit();

			Marshal.ReleaseComObject(target);
			pChildVisual->Release();
			pControlSurface->Release();
			pDCompositionDevice->Release();
			pDXGIDevice->Release();
			pD3D11Device->Release();
			pD3D11DeviceContext->Release();

			var dwAttrib = Convert.ToUInt32(true);

			return
				PInvoke.DwmSetWindowAttribute(
					new((nint)hwnd),
					DWMWINDOWATTRIBUTE.DWMWA_CLOAK,
					&dwAttrib,
					(uint)Marshal.SizeOf(dwAttrib))
				.Succeeded;
		}

		public void UnloadPreview()
		{
			if (hwnd != HWND.NULL)
				DestroyWindow(hwnd);

			//outputLink?.Dispose();
			outputLink = null;

			if (wCls is not null)
				UnregisterClass(wCls.ClassName, Kernel32.GetModuleHandle());
		}

		public unsafe void PointerEntered(bool onPreview)
		{
			if (onPreview)
			{
				var dwAttrib = Convert.ToUInt32(false);

				PInvoke.DwmSetWindowAttribute(
					new((nint)hwnd),
					DWMWINDOWATTRIBUTE.DWMWA_CLOAK,
					&dwAttrib,
					(uint)Marshal.SizeOf(dwAttrib));

				if (isOfficePreview)
					PInvoke.SetWindowLongPtr(new((nint)hwnd), WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, 0);
			}
			else
			{
				PInvoke.SetWindowLongPtr(new((nint)hwnd), WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (nint)(WINDOW_EX_STYLE.WS_EX_LAYERED | WINDOW_EX_STYLE.WS_EX_COMPOSITED));

				var dwAttrib = Convert.ToUInt32(true);

				PInvoke.DwmSetWindowAttribute(
					new((nint)hwnd),
					DWMWINDOWATTRIBUTE.DWMWA_CLOAK,
					&dwAttrib,
					(uint)Marshal.SizeOf(dwAttrib));
			}
		}
	}
}

#pragma warning restore CS8305 // Type is for evaluation purposes only and is subject to change or removal in future updates.
