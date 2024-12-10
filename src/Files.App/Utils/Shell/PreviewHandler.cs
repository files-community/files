using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Vanara.PInvoke;
using Windows.UI;

namespace Files.App.Utils.Shell
{
	/// <summary>
	/// Credits: https://github.com/GeeLaw/PreviewHost/
	/// </summary>
	public sealed class PreviewHandler : IDisposable
	{
		#region IPreviewHandlerFrame support

		[StructLayout(LayoutKind.Sequential)]
		public struct PreviewHandlerFrameInfo
		{
			public IntPtr AcceleratorTableHandle;
			public uint AcceleratorEntryCount;
		}

		[ComImport, Guid("fec87aaf-35f9-447a-adb7-20234491401a"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		public interface IPreviewHandlerFrame
		{
			[PreserveSig]
			HRESULT GetWindowContext(out PreviewHandlerFrameInfo pinfo);
			[PreserveSig]
			HRESULT TranslateAccelerator(ref MSG pmsg);
		}

		public sealed class PreviewHandlerFrame : IPreviewHandlerFrame, IDisposable
		{
			bool disposed;
			nint hwnd;

			public PreviewHandlerFrame(nint frame)
			{
				disposed = true;
				disposed = false;
				hwnd = frame;
			}

			public void Dispose()
			{
				disposed = true;
			}

			public HRESULT GetWindowContext(out PreviewHandlerFrameInfo pinfo)
			{
				pinfo.AcceleratorTableHandle = IntPtr.Zero;
				pinfo.AcceleratorEntryCount = 0;
				if (disposed)
					return HRESULT.E_FAIL;
				return HRESULT.S_OK;
			}

			public HRESULT TranslateAccelerator(ref MSG pmsg)
			{
				if (disposed)
					return HRESULT.E_FAIL;
				return HRESULT.S_FALSE;
			}
		}

		#endregion IPreviewHandlerFrame support

		#region IPreviewHandler major interfaces

		[ComImport, Guid("8895b1c6-b41f-4c1c-a562-0d564250836f"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		interface IPreviewHandler
		{
			[PreserveSig]
			HRESULT SetWindow(IntPtr hwnd, ref RECT prc);
			[PreserveSig]
			HRESULT SetRect(ref RECT prc);
			[PreserveSig]
			HRESULT DoPreview();
			[PreserveSig]
			HRESULT Unload();
			[PreserveSig]
			HRESULT SetFocus();
			[PreserveSig]
			HRESULT QueryFocus(out IntPtr phwnd);
			// TranslateAccelerator is not used here.
		}

		[ComImport, Guid("196bf9a5-b346-4ef0-aa1e-5dcdb76768b1"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		interface IPreviewHandlerVisuals
		{
			[PreserveSig]
			HRESULT SetBackgroundColor(uint color);
			[PreserveSig]
			HRESULT SetFont(ref LOGFONT plf);
			[PreserveSig]
			HRESULT SetTextColor(uint color);
		}

		static uint ColorRefFromColor(Color color)
		{
			return (((uint)color.B) << 16) | (((uint)color.G) << 8) | ((uint)color.R);
		}

		[ComImport, Guid("fc4801a3-2ba9-11cf-a229-00aa003d7352"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		interface IObjectWithSite
		{
			[PreserveSig]
			HRESULT SetSite([In, MarshalAs(UnmanagedType.IUnknown)] object pUnkSite);
			// GetSite is not used.
		}

		#endregion IPreviewHandler major interfaces

		bool disposed;
		bool init;
		bool shown;
		PreviewHandlerFrame comSite;
		nint hwnd;
		IPreviewHandler previewHandler;
		IPreviewHandlerVisuals visuals;
		IntPtr pPreviewHandler;

		public PreviewHandler(Guid clsid, nint frame)
		{
			disposed = true;
			init = false;
			shown = false;
			comSite = new PreviewHandlerFrame(frame);
			hwnd = frame;
			try
			{
				SetupHandler(clsid);
				disposed = false;
			}
			catch
			{
				if (previewHandler != null)
					Marshal.ReleaseComObject(previewHandler);
				previewHandler = null;
				if (pPreviewHandler != IntPtr.Zero)
					Marshal.Release(pPreviewHandler);
				pPreviewHandler = IntPtr.Zero;
				comSite.Dispose();
				comSite = null;
				throw;
			}
		}

		static readonly Guid IPreviewHandlerIid = Guid.ParseExact("8895b1c6-b41f-4c1c-a562-0d564250836f", "d");

		void SetupHandler(Guid clsid)
		{
			IntPtr pph;
			var iid = IPreviewHandlerIid;
			var cannotCreate = "Cannot create class " + clsid.ToString() + " as IPreviewHandler.";
			var cannotCast = "Cannot cast class " + clsid.ToString() + " as IObjectWithSite.";
			var cannotSetSite = "Cannot set site to the preview handler object.";
			// Important: manully calling CoCreateInstance is necessary.
			// If we use Activator.CreateInstance(Type.GetTypeFromCLSID(...)),
			// CLR will allow in-process server, which defeats isolation and
			// creates strange bugs.
			Windows.Win32.Foundation.HRESULT hr2 = Win32PInvoke.CoCreateInstance(ref clsid, IntPtr.Zero, Win32PInvoke.ClassContext.LocalServer, ref iid, out pph);
			HRESULT hr = new(hr2.Value);
			// See https://blogs.msdn.microsoft.com/adioltean/2005/06/24/when-cocreateinstance-returns-0x80080005-co_e_server_exec_failure/
			// CO_E_SERVER_EXEC_FAILURE also tends to happen when debugging in Visual Studio.
			// Moreover, to create the instance in a server at low integrity level, we need
			// to use another thread with low mandatory label. We keep it simple by creating
			// a same-integrity object.
			//if (hr == HRESULT.CO_E_SERVER_EXEC_FAILURE)
			//	hr = CoCreateInstance(ref clsid, IntPtr.Zero, ClassContext.LocalServer, ref iid, out pph);
			if ((int)hr < 0)
				throw new COMException(cannotCreate, (int)hr);
			pPreviewHandler = pph;
			var previewHandlerObject = Marshal.GetUniqueObjectForIUnknown(pph);
			previewHandler = previewHandlerObject as IPreviewHandler;
			if (previewHandler == null)
			{
				Marshal.ReleaseComObject(previewHandlerObject);
				throw new COMException(cannotCreate);
			}
			var objectWithSite = previewHandlerObject as IObjectWithSite;
			if (objectWithSite == null)
				throw new COMException(cannotCast);
			hr = objectWithSite.SetSite(comSite);
			if ((int)hr < 0)
				throw new COMException(cannotSetSite, (int)hr);
			visuals = previewHandlerObject as IPreviewHandlerVisuals;
		}

		#region Initialization interfaces

		[ComImport, Guid("b824b49d-22ac-4161-ac8a-9916e8fa3f7f"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		interface IInitializeWithStream
		{
			[PreserveSig]
			HRESULT Initialize(IStream psi, STGM grfMode);
		}

		[ComImport, Guid("b824b49d-22ac-4161-ac8a-9916e8fa3f7f"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		interface IInitializeWithStreamNative
		{
			[PreserveSig]
			HRESULT Initialize(IntPtr psi, STGM grfMode);
		}

		static readonly Guid IInitializeWithStreamIid = Guid.ParseExact("b824b49d-22ac-4161-ac8a-9916e8fa3f7f", "d");

		[ComImport, Guid("b7d14566-0509-4cce-a71f-0a554233bd9b"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		interface IInitializeWithFile
		{
			[PreserveSig]
			HRESULT Initialize([MarshalAs(UnmanagedType.LPWStr)] string pszFilePath, STGM grfMode);
		}

		static readonly Guid IInitializeWithFileIid = Guid.ParseExact("b7d14566-0509-4cce-a71f-0a554233bd9b", "d");

		[ComImport, Guid("7f73be3f-fb79-493c-a6c7-7ee14e245841"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		interface IInitializeWithItem
		{
			[PreserveSig]
			HRESULT Initialize(IntPtr psi, STGM grfMode);
		}

		static readonly Guid IInitializeWithItemIid = Guid.ParseExact("7f73be3f-fb79-493c-a6c7-7ee14e245841", "d");

		#endregion

		/// <summary>
		/// Tries to initialize the preview handler with an IStream.
		/// </summary>
		/// <exception cref="COMException">This exception is thrown if IInitializeWithStream.Initialize fails for reason other than E_NOTIMPL.</exception>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if mode is neither Read nor ReadWrite.</exception>
		/// <param name="stream">The IStream interface used to initialize the preview handler.</param>
		/// <param name="mode">The storage mode, must be Read or ReadWrite.</param>
		/// <returns>If the handler supports initialization with IStream, true; otherwise, false.</returns>
		public bool InitWithStream(IStream stream, STGM mode)
		{
			if (mode != STGM.STGM_READ && mode != STGM.STGM_READWRITE)
				throw new ArgumentOutOfRangeException("mode", mode, "The argument mode must be Read or ReadWrite.");
			var iws = previewHandler as IInitializeWithStream;
			if (iws == null)
				return false;
			var hr = iws.Initialize(stream, mode);
			if (hr == HRESULT.E_NOTIMPL)
				return false;
			if ((int)hr < 0)
				throw new COMException("IInitializeWithStream.Initialize failed.", (int)hr);
			init = true;
			return true;
		}

		/// <summary>
		/// Same as InitWithStream(IStream, STGM).
		/// </summary>
		/// <exception cref="COMException">See InitWithStream(IStream, STGM).</exception>
		/// <exception cref="ArgumentOutOfRangeException">See InitWithStream(IStream, STGM).</exception>
		/// <param name="pStream">The native pointer to the IStream interface.</param>
		/// <param name="mode">The storage mode.</param>
		/// <returns>True or false, see InitWithStream(IStream, STGM).</returns>
		public bool InitWithStream(IntPtr pStream, STGM mode)
		{
			EnsureNotDisposed();
			EnsureNotInitialized();
			if (mode != STGM.STGM_READ && mode != STGM.STGM_READWRITE)
				throw new ArgumentOutOfRangeException("mode", mode, "The argument mode must be Read or ReadWrite.");
			var iws = previewHandler as IInitializeWithStreamNative;
			if (iws == null)
				return false;
			var hr = iws.Initialize(pStream, mode);
			if (hr == HRESULT.E_NOTIMPL)
				return false;
			if ((int)hr < 0)
				throw new COMException("IInitializeWithStream.Initialize failed.", (int)hr);
			init = true;
			return true;
		}

		/// <summary>
		/// Same as InitWithStream(IStream, STGM).
		/// </summary>
		/// <exception cref="COMException">See InitWithStream(IStream, STGM).</exception>
		/// <exception cref="ArgumentOutOfRangeException">See InitWithStream(IStream, STGM).</exception>
		/// <param name="psi">The native pointer to the IShellItem interface.</param>
		/// <param name="mode">The storage mode.</param>
		/// <returns>True or false, see InitWithStream(IStream, STGM).</returns>
		public bool InitWithItem(IntPtr psi, STGM mode)
		{
			EnsureNotDisposed();
			EnsureNotInitialized();
			if (mode != STGM.STGM_READ && mode != STGM.STGM_READWRITE)
				throw new ArgumentOutOfRangeException("mode", mode, "The argument mode must be Read or ReadWrite.");
			var iwi = previewHandler as IInitializeWithItem;
			if (iwi == null)
				return false;
			var hr = iwi.Initialize(psi, mode);
			if (hr == HRESULT.E_NOTIMPL)
				return false;
			if ((int)hr < 0)
				throw new COMException("IInitializeWithItem.Initialize failed.", (int)hr);
			init = true;
			return true;
		}

		/// <summary>
		/// Same as InitWithStream(IStream, STGM).
		/// </summary>
		/// <exception cref="COMException">See InitWithStream(IStream, STGM).</exception>
		/// <exception cref="ArgumentOutOfRangeException">See InitWithStream(IStream, STGM).</exception>
		/// <param name="path">The path to the file.</param>
		/// <param name="mode">The storage mode.</param>
		/// <returns>True or false, see InitWithStream(IStream, STGM).</returns>
		public bool InitWithFile(string path, STGM mode)
		{
			EnsureNotDisposed();
			EnsureNotInitialized();
			if (mode != STGM.STGM_READ && mode != STGM.STGM_READWRITE)
				throw new ArgumentOutOfRangeException("mode", mode, "The argument mode must be Read or ReadWrite.");
			var iwf = previewHandler as IInitializeWithFile;
			if (iwf == null)
				return false;
			var hr = iwf.Initialize(path, mode);
			if (hr == HRESULT.E_NOTIMPL)
				return false;
			if ((int)hr < 0)
				throw new COMException("IInitializeWithFile.Initialize failed.", (int)hr);
			init = true;
			return true;
		}

		/// <summary>
		/// Tries each way to initialize the object with a file.
		/// </summary>
		/// <param name="path">The file name.</param>
		/// <returns>If initialization was successful, true; otherwise, an exception is thrown.</returns>
		public bool InitWithFileWithEveryWay(string path)
		{
			var exceptions = new List<Exception>();
			var pobj = IntPtr.Zero;
			// Why should we try IStream first?
			// Because that gives us the best security.
			// If we initialize with string or IShellItem,
			// we have no control over how the preview handler
			// opens the file, which might decide to open the
			// file for read/write exclusively.
			try
			{
				pobj = ItemStreamHelper.IStreamFromPath(path);
				if (pobj != IntPtr.Zero
					&& InitWithStream(pobj, STGM.STGM_READ))
					return true;
			}
			catch (Exception ex)
			{
				exceptions.Add(ex);
			}
			finally
			{
				if (pobj != IntPtr.Zero)
					ItemStreamHelper.ReleaseObject(pobj);
				pobj = IntPtr.Zero;
			}
			// Next try file because that could save us some P/Invokes.
			try
			{
				if (InitWithFile(path, STGM.STGM_READ))
					return true;
			}
			catch (Exception ex)
			{
				exceptions.Add(ex);
			}
			try
			{
				pobj = ItemStreamHelper.IShellItemFromPath(path);
				if (pobj != IntPtr.Zero
					&& InitWithItem(pobj, STGM.STGM_READ))
					return true;
				if (exceptions.Count == 0)
					throw new NotSupportedException("The object cannot be initialized at all.");
			}
			catch (Exception ex)
			{
				exceptions.Add(ex);
			}
			finally
			{
				if (pobj != IntPtr.Zero)
					ItemStreamHelper.ReleaseObject(pobj);
				pobj = IntPtr.Zero;
			}
			throw new AggregateException(exceptions);
		}

		/// <summary>
		/// Calls IPreviewHandler.SetWindow.
		/// </summary>
		public bool ResetWindow()
		{
			EnsureNotDisposed();
			//EnsureInitialized();
			if (!init)
				return false;
			var hr = previewHandler.SetWindow(hwnd, new());
			return (int)hr >= 0;
		}

		/// <summary>
		/// Calls IPreviewHandler.SetRect.
		/// </summary>
		public bool ResetBounds(RECT previewerBounds)
		{
			EnsureNotDisposed();
			//EnsureInitialized();
			if (!init)
				return false;
			var hr = previewHandler.SetRect(previewerBounds);
			return (int)hr >= 0;
		}

		/// <summary>
		/// Sets the background if the handler implements IPreviewHandlerVisuals.
		/// </summary>
		/// <param name="color">The background color.</param>
		/// <returns>Whether the call succeeds.</returns>
		public bool SetBackground(Color color)
		{
			var hr = visuals?.SetBackgroundColor(ColorRefFromColor(color));
			return hr.HasValue && (int)hr.Value >= 0;
		}

		/// <summary>
		/// Sets the text color if the handler implements IPreviewHandlerVisuals.
		/// </summary>
		/// <param name="color">The text color.</param>
		/// <returns>Whether the call succeeds.</returns>
		public bool SetForeground(Color color)
		{
			var hr = visuals?.SetTextColor(ColorRefFromColor(color));
			return hr.HasValue && (int)hr.Value >= 0;
		}

		/// <summary>
		/// Sets the font if the handler implements IPreviewHandlerVisuals.
		/// </summary>
		/// <param name="font">The LogFontW reference.</param>
		/// <returns>Whether the call succeeds.</returns>
		public bool SetFont(ref LOGFONT font)
		{
			var hr = visuals?.SetFont(ref font);
			return hr.HasValue && (int)hr.Value >= 0;
		}

		/// <summary>
		/// Shows the preview if the object has been successfully initialized.
		/// </summary>
		public void DoPreview()
		{
			EnsureNotDisposed();
			//EnsureInitialized();
			if (!init)
				return;
			EnsureNotShown();
			ResetWindow();
			previewHandler.DoPreview();
			shown = true;
		}

		/// <summary>
		/// Tells the preview handler to set focus to itself.
		/// </summary>
		public void Focus()
		{
			EnsureNotDisposed();
			//EnsureInitialized();
			if (!init)
				return;
			EnsureShown();
			previewHandler.SetFocus();
		}

		/// <summary>
		/// Tells the preview handler to query focus.
		/// </summary>
		/// <returns>The focused window.</returns>
		public IntPtr QueryFocus()
		{
			EnsureNotDisposed();
			//EnsureInitialized();
			if (!init)
				return IntPtr.Zero;
			EnsureShown();
			IntPtr result;
			var hr = previewHandler.QueryFocus(out result);
			if ((int)hr < 0)
				return IntPtr.Zero;
			return result;
		}

		/// <summary>
		/// Unloads the preview and disposes the object. This method is idempotent.
		/// </summary>
		public void UnloadPreview()
		{
			Dispose(true);
		}

		void EnsureNotDisposed()
		{
			if (disposed)
				throw new ObjectDisposedException("PreviewHandler");
		}

		void EnsureInitialized()
		{
			if (!init)
				throw new InvalidOperationException("Object must be initialized before calling this method.");
		}

		void EnsureNotInitialized()
		{
			if (init)
				throw new InvalidOperationException("Object is already initialized and cannot be initialized again.");
		}

		void EnsureShown()
		{
			if (!shown)
				throw new InvalidOperationException("The preview handler must be shown to call this method.");
		}

		void EnsureNotShown()
		{
			if (shown)
				throw new InvalidOperationException("The preview handler must not be shown to call this method.");
		}

		#region IDisposable pattern

		void Dispose(bool disposing)
		{
			if (disposed)
				return;
			disposed = true;
			init = false;
			if (disposing)
			{
				previewHandler.Unload();
				comSite.Dispose();
				Marshal.ReleaseComObject(previewHandler);
			}
			else
			{
				// We're in the finalizer.
				// Field previewHandler might have been finalized at this point.
				// Get a new RCW.
				var phObject = Marshal.GetUniqueObjectForIUnknown(pPreviewHandler);
				var ph = phObject as IPreviewHandler;
				if (ph != null)
					ph.Unload();
				Marshal.ReleaseComObject(phObject);
			}
			Marshal.Release(pPreviewHandler);
		}

		~PreviewHandler()
		{
			Dispose(false);
		}

		void IDisposable.Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion

	}
}
