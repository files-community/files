using Files.Common;
using System.Diagnostics;
using System.Text;
using Windows.Storage;
using Windows.Storage.Streams;
using static Vanara.PInvoke.Kernel32;
using FileAccess = Vanara.PInvoke.Kernel32.FileAccess;

namespace FilesFullTrust
{
    internal class LogWriter : ILogWriter
    {
        private StorageFile logFile;
        private bool initialized = false;

        public async Task InitializeAsync(string name)
        {
            if (!initialized)
            {
                logFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(name, CreationCollisionOption.OpenIfExists);
                initialized = true;
            }
        }

        public async Task WriteLineToLogAsync(string text)
        {
            if (logFile is null)
            {
                return;
            }
            using var stream = await logFile.OpenAsync(FileAccessMode.ReadWrite, StorageOpenOptions.AllowOnlyReaders);
            using var outputStream = stream.GetOutputStreamAt(stream.Size);
            using var dataWriter = new DataWriter(outputStream);
            dataWriter.WriteString("\n" + text);
            await dataWriter.StoreAsync();
            await outputStream.FlushAsync();

            Debug.WriteLine($"Logged event: {text}");
        }

        public void WriteLineToLog(string text)
        {
            if (logFile is null)
            {
                return;
            }
            using SafeHFILE hStream = CreateFile(logFile.Path,
                FileAccess.GENERIC_WRITE, System.IO.FileShare.Read, null, System.IO.FileMode.OpenOrCreate, Vanara.PInvoke.FileFlagsAndAttributes.FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero);
            if (hStream.IsInvalid)
            {
                return;
            }
            byte[] buff = Encoding.UTF8.GetBytes("\n" + text);
            SetFilePointer(hStream, 0, IntPtr.Zero, System.IO.SeekOrigin.End);
            WriteFile(hStream, buff, (uint)buff.Length, out var dwBytesWritten, IntPtr.Zero);

            Debug.WriteLine($"Logged event: {text}");
        }
    }
}