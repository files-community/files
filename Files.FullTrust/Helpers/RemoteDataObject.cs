using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Forms;
using Vanara.PInvoke;
using STATSTG = System.Runtime.InteropServices.ComTypes.STATSTG;

namespace FilesFullTrust.Helpers
{
    // Class taken from Rx-Explorer (https://github.com/zhuxb711/RX-Explorer)
    public class RemoteDataObject
    {
        /// <summary>
        /// Holds the <see cref="System.Windows.IDataObject"/> that this class is wrapping
        /// </summary>
        private System.Windows.Forms.IDataObject underlyingDataObject;

        /// <summary>
        /// Holds the <see cref="System.Runtime.InteropServices.ComTypes.IDataObject"/> interface to the <see cref="System.Windows.IDataObject"/> that this class is wrapping.
        /// </summary>
        private System.Runtime.InteropServices.ComTypes.IDataObject comUnderlyingDataObject;

        /// <summary>
        /// Holds the internal ole <see cref="System.Windows.IDataObject"/> to the <see cref="System.Windows.IDataObject"/> that this class is wrapping.
        /// </summary>
        private System.Windows.Forms.IDataObject oleUnderlyingDataObject;

        /// <summary>
        /// Holds the <see cref="MethodInfo"/> of the "GetDataFromHGLOBAL" method of the internal ole <see cref="System.Windows.IDataObject"/>.
        /// </summary>
        private MethodInfo getDataFromHGLOBALMethod;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="underlyingDataObject">The underlying data object to wrap.</param>
        public RemoteDataObject(System.Windows.Forms.IDataObject underlyingDataObject)
        {
            //get the underlying dataobject and its ComType IDataObject interface to it
            this.underlyingDataObject = underlyingDataObject;
            comUnderlyingDataObject = (System.Runtime.InteropServices.ComTypes.IDataObject)this.underlyingDataObject;

            //get the internal ole dataobject and its GetDataFromHGLOBAL so it can be called later
            FieldInfo innerDataField = this.underlyingDataObject.GetType().GetField("innerData", BindingFlags.NonPublic | BindingFlags.Instance);
            oleUnderlyingDataObject = (System.Windows.Forms.IDataObject)innerDataField.GetValue(this.underlyingDataObject);
            getDataFromHGLOBALMethod = oleUnderlyingDataObject.GetType().GetMethod("GetDataFromHGLOBAL", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public IEnumerable<DataPackage> GetRemoteData()
        {
            string FormatName = string.Empty;

            if (GetDataPresent(Shell32.ShellClipboardFormat.CFSTR_FILEDESCRIPTORW))
            {
                FormatName = Shell32.ShellClipboardFormat.CFSTR_FILEDESCRIPTORW;
            }
            else if (GetDataPresent(Shell32.ShellClipboardFormat.CFSTR_FILEDESCRIPTORA))
            {
                FormatName = Shell32.ShellClipboardFormat.CFSTR_FILEDESCRIPTORA;
            }

            if (string.IsNullOrEmpty(FormatName))
            {
                yield break;
            }
            else
            {
                if (underlyingDataObject.GetData(FormatName, true) is MemoryStream FileGroupDescriptorStream)
                {
                    try
                    {
                        byte[] FileGroupDescriptorBytes = FileGroupDescriptorStream.ToArray();

                        IntPtr FileGroupDescriptorAPointer = Marshal.AllocHGlobal(FileGroupDescriptorBytes.Length);

                        try
                        {
                            Marshal.Copy(FileGroupDescriptorBytes, 0, FileGroupDescriptorAPointer, FileGroupDescriptorBytes.Length);

                            int ItemCount = Marshal.ReadInt32(FileGroupDescriptorAPointer);

                            IntPtr FileDescriptorPointer = (IntPtr)(FileGroupDescriptorAPointer.ToInt64() + Marshal.SizeOf(ItemCount));

                            for (int FileDescriptorIndex = 0; FileDescriptorIndex < ItemCount; FileDescriptorIndex++)
                            {
                                Shell32.FILEDESCRIPTOR FileDescriptor = Marshal.PtrToStructure<Shell32.FILEDESCRIPTOR>(FileDescriptorPointer);

                                if (FileDescriptor.dwFileAttributes.HasFlag(FileFlagsAndAttributes.FILE_ATTRIBUTE_DIRECTORY))
                                {
                                    yield return new DataPackage(FileDescriptor.cFileName, StorageType.Directroy, null);
                                }
                                else
                                {
                                    yield return new DataPackage(FileDescriptor.cFileName, StorageType.File, GetContentData(Shell32.ShellClipboardFormat.CFSTR_FILECONTENTS, FileDescriptorIndex));
                                }

                                FileDescriptorPointer = (IntPtr)(FileDescriptorPointer.ToInt64() + Marshal.SizeOf(FileDescriptor));
                            }
                        }
                        finally
                        {
                            Marshal.FreeHGlobal(FileGroupDescriptorAPointer);
                        }
                    }
                    finally
                    {
                        FileGroupDescriptorStream.Dispose();
                    }
                }
                else
                {
                    yield break;
                }
            }
        }

        /// <summary>
        /// Retrieves the data associated with the specified data format at the specified index.
        /// </summary>
        /// <param name="Format">The format of the data to retrieve. See <see cref="T:System.Windows.DataFormats"></see> for predefined formats.</param>
        /// <param name="Index">The index of the data to retrieve.</param>
        /// <returns>
        /// A <see cref="MemoryStream"/> containing the raw data for the specified data format at the specified index.
        /// </returns>
        private MemoryStream GetContentData(string Format, int Index)
        {
            //create a FORMATETC struct to request the data with
            FORMATETC Formatetc = new FORMATETC
            {
                cfFormat = (short)DataFormats.GetFormat(Format).Id,
                dwAspect = DVASPECT.DVASPECT_CONTENT,
                lindex = Index,
                ptd = new IntPtr(0),
                tymed = TYMED.TYMED_ISTREAM | TYMED.TYMED_ISTORAGE | TYMED.TYMED_HGLOBAL
            };

            //using the Com IDataObject interface get the data using the defined FORMATETC
            comUnderlyingDataObject.GetData(ref Formatetc, out STGMEDIUM Medium);

            //retrieve the data depending on the returned store type
            switch (Medium.tymed)
            {
                case TYMED.TYMED_ISTORAGE:
                    {
                        //to handle a IStorage it needs to be written into a second unmanaged
                        //memory mapped storage and then the data can be read from memory into
                        //a managed byte and returned as a MemoryStream

                        try
                        {
                            //marshal the returned pointer to a IStorage object
                            Ole32.IStorage IStorageObject = (Ole32.IStorage)Marshal.GetObjectForIUnknown(Medium.unionmember);

                            try
                            {
                                //create a ILockBytes (unmanaged byte array) and then create a IStorage using the byte array as a backing store
                                Ole32.CreateILockBytesOnHGlobal(IntPtr.Zero, true, out Ole32.ILockBytes LockBytes);
                                Ole32.StgCreateDocfileOnILockBytes(LockBytes, STGM.STGM_READWRITE | STGM.STGM_SHARE_EXCLUSIVE | STGM.STGM_CREATE, ppstgOpen: out Ole32.IStorage IStorageObjectCopy);

                                try
                                {
                                    //copy the returned IStorage into the new IStorage
                                    IStorageObject.CopyTo(snbExclude: IntPtr.Zero, pstgDest: IStorageObjectCopy);
                                    LockBytes.Flush();
                                    IStorageObjectCopy.Commit(Ole32.STGC.STGC_DEFAULT);

                                    //get the STATSTG of the LockBytes to determine how many bytes were written to it
                                    LockBytes.Stat(out STATSTG LockBytesStat, Ole32.STATFLAG.STATFLAG_NONAME);

                                    int CbSize = Convert.ToInt32(LockBytesStat.cbSize);

                                    IntPtr LockBytesContentPtr = Marshal.AllocHGlobal(CbSize);

                                    try
                                    {
                                        LockBytes.ReadAt(0, LockBytesContentPtr, Convert.ToUInt32(LockBytesStat.cbSize), out _);

                                        byte[] LockBytesContent = new byte[CbSize];

                                        Marshal.Copy(LockBytesContentPtr, LockBytesContent, 0, LockBytesContent.Length);

                                        return new MemoryStream(LockBytesContent);
                                    }
                                    finally
                                    {
                                        Marshal.FreeHGlobal(LockBytesContentPtr);
                                    }
                                }
                                finally
                                {
                                    Marshal.ReleaseComObject(IStorageObjectCopy);
                                    Marshal.ReleaseComObject(LockBytes);
                                }
                            }
                            finally
                            {
                                Marshal.ReleaseComObject(IStorageObject);
                            }
                        }
                        finally
                        {
                            Marshal.Release(Medium.unionmember);
                        }
                    }
                case TYMED.TYMED_ISTREAM:
                    {
                        //to handle a IStream it needs to be read into a managed byte and
                        //returned as a MemoryStream

                        IStream IStreamObject = (IStream)Marshal.GetObjectForIUnknown(Medium.unionmember);

                        try
                        {
                            //get the STATSTG of the IStream to determine how many bytes are in it
                            IStreamObject.Stat(out STATSTG iStreamStat, 0);

                            byte[] IStreamContent = new byte[(Convert.ToInt32(iStreamStat.cbSize))];

                            IStreamObject.Read(IStreamContent, IStreamContent.Length, IntPtr.Zero);

                            return new MemoryStream(IStreamContent);
                        }
                        finally
                        {
                            Marshal.Release(Medium.unionmember);
                            Marshal.ReleaseComObject(IStreamObject);
                        }
                    }
                case TYMED.TYMED_HGLOBAL:
                    {
                        //to handle a HGlobal the exisitng "GetDataFromHGLOBAL" method is invoked via
                        //reflection

                        try
                        {
                            return (MemoryStream)getDataFromHGLOBALMethod.Invoke(oleUnderlyingDataObject, new object[] { DataFormats.GetFormat(Formatetc.cfFormat).Name, Medium.unionmember });
                        }
                        finally
                        {
                            Marshal.Release(Medium.unionmember);
                        }
                    }
                default:
                    {
                        return null;
                    }
            }
        }

        /// <summary>
        /// Determines whether data stored in this instance is associated with, or can be converted to, the specified format.
        /// </summary>
        /// <param name="format">The format for which to check. See <see cref="T:System.Windows.DataFormats"></see> for predefined formats.</param>
        /// <returns>
        /// true if data stored in this instance is associated with, or can be converted to, the specified format; otherwise false.
        /// </returns>
        public bool GetDataPresent(string format)
        {
            return underlyingDataObject.GetDataPresent(format);
        }

        public sealed class DataPackage : IDisposable
        {
            public StorageType ItemType { get; }

            public MemoryStream ContentStream { get; }

            public string Name { get; }

            public DataPackage(string Name, StorageType ItemType, MemoryStream ContentStream)
            {
                this.Name = Name;
                this.ItemType = ItemType;
                this.ContentStream = ContentStream;
            }

            public void Dispose()
            {
                GC.SuppressFinalize(this);
                ContentStream?.Dispose();
            }

            ~DataPackage()
            {
                Dispose();
            }
        }

        public enum StorageType
        {
            File = 0,
            Directroy = 1
        }
    }
}