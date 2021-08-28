using Files.Common;
using System.IO.Pipes;
using System.Security.Principal;
using Windows.Foundation.Collections;

namespace FilesFullTrust.MessageHandlers
{
    public class QuickLookHandler : IMessageHandler
    {
        private static readonly Logger Logger = Program.Logger;

        public void Initialize(NamedPipeServerStream connection)
        {
        }

        public async Task ParseArgumentsAsync(NamedPipeServerStream connection, Dictionary<string, object> message, string arguments)
        {
            switch (arguments)
            {
                case "DetectQuickLook":
                    // Check QuickLook Availability
                    var available = CheckQuickLookAvailability();
                    await Win32API.SendMessageAsync(connection, new ValueSet() { { "IsAvailable", available } }, message.Get("RequestID", (string)null));
                    break;

                case "ToggleQuickLook":
                    var path = (string)message["path"];
                    ToggleQuickLook(path);
                    break;
            }
        }

        public void ToggleQuickLook(string path)
        {
            Logger.Info("Toggle QuickLook");

            string PipeName = $"QuickLook.App.Pipe.{WindowsIdentity.GetCurrent().User?.Value}";
            string Toggle = "QuickLook.App.PipeMessages.Toggle";

            using (var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
            {
                client.Connect();

                using (var writer = new StreamWriter(client))
                {
                    writer.WriteLine($"{Toggle}|{path}");
                    writer.Flush();
                }
            }
        }

        public bool CheckQuickLookAvailability()
        {
            static int QuickLookServerAvailable()
            {
                string PipeName = $"QuickLook.App.Pipe.{WindowsIdentity.GetCurrent().User?.Value}";
                string Switch = "QuickLook.App.PipeMessages.Switch";

                using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                try
                {
                    client.Connect(500);
                    var serverInstances = client.NumberOfServerInstances;

                    using (var writer = new StreamWriter(client))
                    {
                        writer.WriteLine($"{Switch}|");
                        writer.Flush();
                    }

                    return serverInstances;
                }
                catch (TimeoutException)
                {
                    client.Close();
                    return 0;
                }
            }

            try
            {
                var result = QuickLookServerAvailable();
                Logger.Info($"QuickLook detected: {result != 0}");
                return result != 0;
            }
            catch (Exception ex)
            {
                Logger.Info(ex, ex.Message);
                return false;
            }
        }

        public void Dispose()
        {
        }
    }
}