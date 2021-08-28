using System.IO.Pipes;

namespace FilesFullTrust.MessageHandlers
{
    public interface IMessageHandler : IDisposable
    {
        Task ParseArgumentsAsync(NamedPipeServerStream connection, Dictionary<string, object> message, string arguments);

        void Initialize(NamedPipeServerStream connection);
    }
}
