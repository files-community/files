using Files.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using Windows.Storage;

namespace FilesFullTrust.MessageHandlers
{
    public class FileTagsHandler : IMessageHandler
    {
        public static string ReadFileTag(string filePath)
        {
            using var hStream = Kernel32.CreateFile($"{filePath}:files",
                Kernel32.FileAccess.GENERIC_READ, 0, null, FileMode.Open, FileFlagsAndAttributes.FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero);
            if (hStream.IsInvalid)
            {
                return null;
            }
            var bytes = new byte[4096];
            var ret = Kernel32.ReadFile(hStream, bytes, (uint)bytes.Length, out var read, IntPtr.Zero);
            if (!ret)
            {
                return null;
            }
            return System.Text.Encoding.UTF8.GetString(bytes, 0, (int)read);
        }

        public static bool WriteFileTag(string filePath, string tag)
        {
            if (tag == null)
            {
                return Kernel32.DeleteFile($"{filePath}:files");
            }
            else
            {
                using var hStream = Kernel32.CreateFile($"{filePath}:files",
                    Kernel32.FileAccess.GENERIC_WRITE, 0, null, FileMode.Create, FileFlagsAndAttributes.FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero);
                if (hStream.IsInvalid)
                {
                    return false;
                }
                byte[] buff = System.Text.Encoding.UTF8.GetBytes(tag);
                return Kernel32.WriteFile(hStream, buff, (uint)buff.Length, out var written, IntPtr.Zero);
            }
        }

        public static ulong? GetFileFRN(string filePath)
        {
            //using var si = new ShellItem(filePath);
            //return (ulong?)si.Properties["System.FileFRN"]; // Leaves open file handles
            using var hFile = Kernel32.CreateFile(filePath, Kernel32.FileAccess.GENERIC_READ, FileShare.ReadWrite, null, FileMode.Open, FileFlagsAndAttributes.FILE_FLAG_BACKUP_SEMANTICS);
            if (hFile.IsInvalid)
            {
                return null;
            }
            ulong? frn = null;
            Extensions.IgnoreExceptions(() =>
            {
                var fileID = Kernel32.GetFileInformationByHandleEx<Kernel32.FILE_ID_INFO>(hFile, Kernel32.FILE_INFO_BY_HANDLE_CLASS.FileIdInfo);
                frn = BitConverter.ToUInt64(fileID.FileId.Identifier, 0);
            });
            return frn;
        }

        public void UpdateTagsDb()
        {
            string fileTagsDbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "filetags.db");
            using var dbInstance = new Common.FileTagsDb(fileTagsDbPath, true);
            foreach (var file in dbInstance.GetAll())
            {
                var pathFromFrn = Win32API.PathFromFileId(file.Frn ?? 0, file.FilePath);
                if (pathFromFrn != null)
                {
                    // Frn is valid, update file path
                    var tag = ReadFileTag(pathFromFrn.Replace(@"\\?\", ""));
                    if (tag != null)
                    {
                        dbInstance.UpdateTag(file.Frn ?? 0, null, pathFromFrn.Replace(@"\\?\", ""));
                        dbInstance.SetTag(pathFromFrn.Replace(@"\\?\", ""), file.Frn, tag);
                    }
                    else
                    {
                        dbInstance.SetTag(null, file.Frn, null);
                    }
                }
                else
                {
                    var tag = ReadFileTag(file.FilePath);
                    if (tag != null)
                    {
                        if (!Extensions.IgnoreExceptions(() =>
                        {
                            var frn = GetFileFRN(file.FilePath);
                            dbInstance.UpdateTag(file.FilePath, frn, null);
                            dbInstance.SetTag(file.FilePath, (ulong?)frn, tag);
                        }, Program.Logger))
                        {
                            dbInstance.SetTag(file.FilePath, null, null);
                        }
                    }
                    else
                    {
                        dbInstance.SetTag(file.FilePath, null, null);
                    }
                }
            }
        }

        public Task ParseArgumentsAsync(NamedPipeServerStream connection, Dictionary<string, object> message, string arguments)
        {
            switch (arguments)
            {
                case "UpdateTagsDb":
                    UpdateTagsDb();
                    break;
            }
            return Task.CompletedTask;
        }

        public void Initialize(NamedPipeServerStream connection)
        {
        }

        public void Dispose()
        {
        }
    }
}
