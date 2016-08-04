using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Razevolution.Tooling
{
    public class RazorServer
    {
        private readonly TcpClient _client;
        private readonly MessageReader _messageReader;
        private readonly MessageQueue _queue;
        private readonly BinaryReader _reader;
        private readonly NetworkStream _stream;
        private readonly BinaryWriter _writer;

        public RazorServer(TcpClient client)
        {
            _client = client;

            _stream = _client.GetStream();

            _reader = new BinaryReader(_stream);
            _writer = new BinaryWriter(_stream);

            _queue = new MessageQueue();
            _messageReader = new DefaultMessageReader(_reader, _queue);
        }

        public void Start()
        {
            _messageReader.Start();
        }
    }
}
