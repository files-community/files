using Files.Common;
using FilesFullTrust.MessageHandlers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace FilesFullTrust
{
    internal class Program
    {
        public static Logger Logger { get; private set; }
        public static JsonSerializerOptions IncludeFieldsOptions = new JsonSerializerOptions { IncludeFields = true };
        private static readonly LogWriter logWriter = new LogWriter();

        [STAThread]
        private static async Task Main(string[] args)
        {
            Logger = new Logger(logWriter);
            await logWriter.InitializeAsync("debug_fulltrust.log");
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;

            if (HandleCommandLineArgs())
            {
                // Handles OpenShellCommandInExplorer
                return;
            }

            try
            {
                // Create message handlers
                messageHandlers = new List<IMessageHandler>();
                messageHandlers.Add(new RecycleBinHandler());
                messageHandlers.Add(new LibrariesHandler());
                messageHandlers.Add(new FileTagsHandler());
                messageHandlers.Add(new ApplicationLaunchHandler());
                messageHandlers.Add(new NetworkDrivesHandler());
                messageHandlers.Add(new FileOperationsHandler());
                messageHandlers.Add(new ContextMenuHandler());
                messageHandlers.Add(new QuickLookHandler());
                messageHandlers.Add(new Win32MessageHandler());

                // Connect to app service and wait until the connection gets closed
                appServiceExit = new ManualResetEvent(false);
                InitializeAppServiceConnection();

                // Initialize message handlers
                messageHandlers.ForEach(mh => mh.Initialize(connection));

                // Initialize device watcher
                deviceWatcher = new DeviceWatcher(connection);
                deviceWatcher.Start();

                // Update tags db
                messageHandlers.OfType<FileTagsHandler>().Single().UpdateTagsDb();

                // Wait until the connection gets closed
                appServiceExit.WaitOne();
            }
            finally
            {
                messageHandlers.ForEach(mh => mh.Dispose());
                deviceWatcher?.Dispose();
                connection?.Dispose();
                appServiceExit?.Dispose();
            }
        }

        private static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            Logger.UnhandledError(exception, exception.Message);
        }

        private static NamedPipeServerStream connection;
        private static ManualResetEvent appServiceExit;
        private static DeviceWatcher deviceWatcher;
        private static List<IMessageHandler> messageHandlers;

        private static async void InitializeAppServiceConnection()
        {
            connection = NamedPipeServerStreamConstructors.New($@"FilesInteropService_ServerPipe", PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Message, PipeOptions.Asynchronous, 2048, 2048, null, HandleInheritability.None, PipeAccessRights.ChangePermissions);

            PipeSecurity Security = connection.GetAccessControl();
            PipeAccessRule ClientRule = new PipeAccessRule(new SecurityIdentifier("S-1-15-2-1"), PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance, AccessControlType.Allow);
            PipeAccessRule OwnerRule = new PipeAccessRule(WindowsIdentity.GetCurrent().Owner, PipeAccessRights.FullControl, AccessControlType.Allow);
            Security.AddAccessRule(ClientRule);
            Security.AddAccessRule(OwnerRule);
            if (IsAdministrator())
            {
                PipeAccessRule EveryoneRule = new PipeAccessRule(new SecurityIdentifier("S-1-1-0"), PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance, AccessControlType.Allow);
                Security.AddAccessRule(EveryoneRule); // TODO: find the minimum permission to allow connection when admin
            }
            connection.SetAccessControl(Security);

            try
            {
                using var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromSeconds(10));
                await connection.WaitForConnectionAsync(cts.Token);
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Could not initialize pipe!");
            }

            if (connection.IsConnected)
            {
                connection.ReadMode = PipeTransmissionMode.Message;
                var info = (Buffer: new byte[connection.InBufferSize], Message: new StringBuilder());
                BeginRead(info);
            }
            else
            {
                appServiceExit.Set();
            }
        }

        private static void BeginRead((byte[] Buffer, StringBuilder Message) info)
        {
            var isConnected = connection.IsConnected;
            if (isConnected)
            {
                try
                {
                    connection.BeginRead(info.Buffer, 0, info.Buffer.Length, EndReadCallBack, info);
                }
                catch
                {
                    isConnected = false;
                }
            }
            if (!isConnected)
            {
                appServiceExit.Set();
            }
        }

        private static void EndReadCallBack(IAsyncResult result)
        {
            var info = ((byte[] Buffer, StringBuilder Message))result.AsyncState;
            var readBytes = connection.EndRead(result);
            if (readBytes > 0)
            {
                // Get the read bytes and append them
                info.Message.Append(Encoding.UTF8.GetString(info.Buffer, 0, readBytes));

                if (connection.IsMessageComplete) // Message is completed
                {
                    var message = info.Message.ToString().TrimEnd('\0');
                    var options = new JsonSerializerOptions
                    {
                        Converters =
                        {
                            new DictionaryStringObjectJsonConverter()
                        },
                        IncludeFields = true
                    };
                    Connection_RequestReceived(connection, JsonSerializer.Deserialize<Dictionary<string, object>>(message, options));

                    // Begin a new reading operation
                    var nextInfo = (Buffer: new byte[connection.InBufferSize], Message: new StringBuilder());
                    BeginRead(nextInfo);

                    return;
                }
            }
            BeginRead(info);
        }

        private static async void Connection_RequestReceived(NamedPipeServerStream conn, Dictionary<string, object> message)
        {
            // Get a deferral because we use an awaitable API below to respond to the message
            // and we don't want this call to get cancelled while we are waiting.
            if (message == null)
            {
                return;
            }

            if (message.ContainsKey("Arguments"))
            {
                // This replaces launching the fulltrust process with arguments
                // Instead a single instance of the process is running
                // Requests from UWP app are sent via AppService connection
                var arguments = (string)message["Arguments"];
                Logger.Info($"Argument: {arguments}");

                await ParseArgumentsAsync(message, arguments);
            }
        }

        private static bool IsAdministrator()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static async Task ParseArgumentsAsync(Dictionary<string, object> message, string arguments)
        {
            switch (arguments)
            {
                case "Terminate":
                    // Exit fulltrust process (UWP is closed or suspended)
                    appServiceExit.Set();
                    break;

                case "Elevate":
                    // Relaunch fulltrust process as admin
                    if (!IsAdministrator())
                    {
                        try
                        {
                            using (Process elevatedProcess = new Process())
                            {
                                elevatedProcess.StartInfo.Verb = "runas";
                                elevatedProcess.StartInfo.UseShellExecute = true;
                                elevatedProcess.StartInfo.FileName = Process.GetCurrentProcess().MainModule.FileName;
                                elevatedProcess.StartInfo.Arguments = "elevate";
                                elevatedProcess.Start();
                            }
                            await Win32API.SendMessageAsync(connection, new ValueSet() { { "Success", 0 } }, message.Get("RequestID", (string)null));
                            appServiceExit.Set();
                        }
                        catch (Win32Exception)
                        {
                            // If user cancels UAC
                            await Win32API.SendMessageAsync(connection, new ValueSet() { { "Success", 1 } }, message.Get("RequestID", (string)null));
                        }
                    }
                    else
                    {
                        await Win32API.SendMessageAsync(connection, new ValueSet() { { "Success", -1 } }, message.Get("RequestID", (string)null));
                    }
                    break;

                default:
                    foreach (var mh in messageHandlers)
                    {
                        await mh.ParseArgumentsAsync(connection, message, arguments);
                    }
                    break;
            }
        }

        private static bool HandleCommandLineArgs()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            var arguments = (string)localSettings.Values["Arguments"];
            if (!string.IsNullOrWhiteSpace(arguments))
            {
                localSettings.Values.Remove("Arguments");

                if (arguments == "ShellCommand")
                {
                    // Kill the process. This is a BRUTAL WAY to kill a process.
#if DEBUG
                    // In debug mode this kills this process too??
#else
                    var pid = (int)localSettings.Values["pid"];
                    Process.GetProcessById(pid).Kill();
#endif

                    using Process process = new Process();
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.FileName = "explorer.exe";
                    process.StartInfo.CreateNoWindow = false;
                    process.StartInfo.Arguments = (string)localSettings.Values["ShellCommand"];
                    process.Start();

                    return true;
                }
            }
            return false;
        }
    }

    public class DictionaryStringObjectJsonConverter : JsonConverter<Dictionary<string, object>>
    {
        public override Dictionary<string, object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"JsonTokenType was of type {reader.TokenType}, only objects are supported");
            }

            var dictionary = new Dictionary<string, object>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return dictionary;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("JsonTokenType was not PropertyName");
                }

                var propertyName = reader.GetString();

                if (string.IsNullOrWhiteSpace(propertyName))
                {
                    throw new JsonException("Failed to get property name");
                }

                reader.Read();

                dictionary.Add(propertyName, ExtractValue(ref reader, options));
            }

            return dictionary;
        }

        public override void Write(Utf8JsonWriter writer, Dictionary<string, object> value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, options);
        }

        private object ExtractValue(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    if (reader.TryGetDateTime(out var date))
                    {
                        return date;
                    }
                    return reader.GetString();
                case JsonTokenType.False:
                    return false;
                case JsonTokenType.True:
                    return true;
                case JsonTokenType.Null:
                    return null;
                case JsonTokenType.Number:
                    if (reader.TryGetInt64(out var result))
                    {
                        return result;
                    }
                    return reader.GetDecimal();
                case JsonTokenType.StartObject:
                    return Read(ref reader, null, options);
                case JsonTokenType.StartArray:
                    var list = new List<object>();
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    {
                        list.Add(ExtractValue(ref reader, options));
                    }
                    return list;
                default:
                    throw new JsonException($"'{reader.TokenType}' is not supported");
            }
        }
    }
}