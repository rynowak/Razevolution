using System.IO;
using Newtonsoft.Json;
using Razevolution.Tooling.Messages;

namespace Razevolution.VSTools
{
    public class ProjectServerClient
    {
        private readonly object _lock;
        private readonly BinaryWriter _writer;

        public ProjectServerClient(BinaryWriter writer)
        {
            _writer = writer;

            _lock = new object();
        }

        public void Send(string type, Message message)
        {
            lock (_lock)
            {
                _writer.Write(JsonConvert.SerializeObject(new { type = type, body = message }));
            }
        }
    }
}
