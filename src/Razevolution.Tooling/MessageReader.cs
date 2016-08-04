using System;
using System.IO;
using System.Threading;
using Newtonsoft.Json.Linq;
using Razevolution.Tooling.Messages;

namespace Razevolution.Tooling
{
    public abstract class MessageReader
    {
        private readonly CancellationTokenSource _cancel;
        private readonly object _lock;
        private readonly MessageQueue _queue;
        private readonly BinaryReader _reader;
        private readonly Thread _thread;

        protected MessageReader(BinaryReader reader, MessageQueue queue)
        {
            _reader = reader;
            _queue = queue;

            _lock = new object();
            _cancel = new CancellationTokenSource();
            _thread = new Thread(ReadMessages) { IsBackground = true };
        }

        public void Start()
        {
            lock (_lock)
            {
                _thread.Start();
            }
        }

        public void Wait()
        {
            if (!_thread.IsAlive)
            {
                throw new InvalidOperationException();
            }

            _thread.Join();
        }

        public void Stop()
        {
            if (!_thread.IsAlive)
            {
                throw new InvalidOperationException();
            }

            _cancel.Cancel();
            _thread.Join();
        }

        protected abstract Message DeserializeBody(string type, JObject body);

        private void ReadMessages(object state)
        {
            while (!_cancel.IsCancellationRequested)
            {
                string text;

                try
                {
                    // Blocking read
                    text = _reader.ReadString();
                }
                catch (IOException)
                {
                    Console.WriteLine("shutting down");
                    _cancel.Cancel();
                    return;
                }

                ReadMessage(text);
            }
        }

        private void ReadMessage(string text)
        {
            var jObject = JObject.Parse(text);

            var type = jObject.Property("type");
            var body = jObject.Property("body");
            if (type == null ||
                type.Value.Type != JTokenType.String ||
                body == null ||
                body.Value.Type != JTokenType.Object)
            {
                Console.WriteLine($"parse error: {text}");

                _queue.Enqueue(new ErrorMessage()
                {
                    OriginalText = text,
                });
                return;
            }

            try
            {
                var message = DeserializeBody((string)type.Value, (JObject)body.Value);
                Console.WriteLine($"got message {message}");

                _queue.Enqueue(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"error: {text} {ex}");

                _queue.Enqueue(new ErrorMessage()
                {
                    OriginalText = text,
                    Exception = ex,
                });
            }
        }
    }
}
