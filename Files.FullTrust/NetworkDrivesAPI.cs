using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Vanara.Extensions;
using Vanara.InteropServices;
using static Vanara.PInvoke.Mpr;

namespace FilesFullTrust
{
    public class NetworkDrivesAPI
    {
        /// <summary>
        /// A dialog box that allows the user to browse and connect to network resources.
        /// </summary>
        public class NetworkConnectionDialog : CommonDialog
        {
            private NETRESOURCE nres = new NETRESOURCE();
            private CONNECTDLGSTRUCT opts;

            /// <summary>Initializes a new instance of the <see cref="NetworkConnectionDialog"/> class.</summary>
            public NetworkConnectionDialog()
            {
                opts.cbStructure = (uint)Marshal.SizeOf(typeof(CONNECTDLGSTRUCT));
                nres.dwType = NETRESOURCEType.RESOURCETYPE_DISK;
            }

            /// <summary>Gets the connected device number. This value is only valid after successfully running the dialog.</summary>
            /// <value>The connected device number. The value is 1 for A:, 2 for B:, 3 for C:, and so on. If the user made a deviceless connection, the value is –1.</value>
            [Browsable(false)]
            public int ConnectedDeviceNumber => opts.dwDevNum;

            /// <summary>Gets or sets a value indicating whether to hide the check box allowing the user to restore the connection at logon.</summary>
            /// <value><c>true</c> if hiding restore connection check box; otherwise, <c>false</c>.</value>
            [DefaultValue(false), Category("Appearance"), Description("Hide the check box allowing the user to restore the connection at logon.")]
            public bool HideRestoreConnectionCheckBox
            {
                get => opts.dwFlags.IsFlagSet(CONN_DLG.CONNDLG_HIDE_BOX);
                set => opts.dwFlags = opts.dwFlags.SetFlags(CONN_DLG.CONNDLG_HIDE_BOX, value);
            }

            /// <summary>Gets or sets a value indicating whether restore the connection at logon.</summary>
            /// <value><c>true</c> to restore connection at logon; otherwise, <c>false</c>.</value>
            [DefaultValue(false), Category("Behavior"), Description("Restore the connection at logon.")]
            public bool PersistConnectionAtLogon
            {
                get => opts.dwFlags.IsFlagSet(CONN_DLG.CONNDLG_PERSIST);
                set
                {
                    opts.dwFlags = opts.dwFlags.SetFlags(CONN_DLG.CONNDLG_PERSIST, value);
                    opts.dwFlags = opts.dwFlags.SetFlags(CONN_DLG.CONNDLG_NOT_PERSIST, !value);
                }
            }

            /// <summary>
            /// Gets or sets a value indicating whether to display a read-only path instead of allowing the user to type in a path. This is only
            /// valid if <see cref="RemoteNetworkName"/> is not <see langword="null"/>.
            /// </summary>
            /// <value><c>true</c> to display a read only path; otherwise, <c>false</c>.</value>
            [DefaultValue(false), Category("Appearance"), Description("Display a read-only path instead of allowing the user to type in a path.")]
            public bool ReadOnlyPath { get; set; }

            /// <summary>Gets or sets the name of the remote network.</summary>
            /// <value>The name of the remote network.</value>
            [DefaultValue(null), Category("Behavior"), Description("The value displayed in the path field.")]
            public string RemoteNetworkName { get => nres.lpRemoteName; set => nres.lpRemoteName = value; }

            /// <summary>Gets or sets a value indicating whether to enter the most recently used paths into the combination box.</summary>
            /// <value><c>true</c> to use MRU path; otherwise, <c>false</c>.</value>
            /// <exception cref="InvalidOperationException">UseMostRecentPath</exception>
            [DefaultValue(false), Category("Behavior"), Description("Enter the most recently used paths into the combination box.")]
            public bool UseMostRecentPath
            {
                get => opts.dwFlags.IsFlagSet(CONN_DLG.CONNDLG_USE_MRU);
                set
                {
                    if (value && !string.IsNullOrEmpty(RemoteNetworkName))
                        throw new InvalidOperationException($"{nameof(UseMostRecentPath)} cannot be set to true if {nameof(RemoteNetworkName)} has a value.");
                    opts.dwFlags = opts.dwFlags.SetFlags(CONN_DLG.CONNDLG_USE_MRU, value);
                }
            }

            /// <inheritdoc/>
            public override void Reset()
            {
                opts.dwDevNum = -1;
                opts.dwFlags = 0;
                opts.lpConnRes = IntPtr.Zero;
                ReadOnlyPath = false;
            }

            /// <inheritdoc/>
            protected override bool RunDialog(IntPtr hwndOwner)
            {
                using (var lpnres = SafeCoTaskMemHandle.CreateFromStructure(nres))
                {
                    opts.hwndOwner = hwndOwner;
                    opts.lpConnRes = lpnres.DangerousGetHandle();
                    if (ReadOnlyPath && !string.IsNullOrEmpty(nres.lpRemoteName))
                        opts.dwFlags |= CONN_DLG.CONNDLG_RO_PATH;
                    var ret = WNetConnectionDialog1(opts);
                    opts.lpConnRes = IntPtr.Zero;
                    if (ret == unchecked((uint)-1)) return false;
                    ret.ThrowIfFailed();
                    return true;
                }
            }
        }

        public static async Task<bool> OpenMapNetworkDriveDialog(long hwnd)
        {
            return await Win32API.StartSTATask(() =>
            {
                using var ncd = new NetworkConnectionDialog { UseMostRecentPath = true };
                ncd.HideRestoreConnectionCheckBox = false;
                return ncd.ShowDialog(Win32API.Win32Window.FromLong(hwnd)) == DialogResult.OK;
            });
        }

        public static bool DisconnectNetworkDrive(string drive)
        {
            return WNetCancelConnection2(drive.TrimEnd('\\'), CONNECT.CONNECT_UPDATE_PROFILE, true).Succeeded;
        }
    }
}