using System.Collections.Concurrent;
using Razevolution.Tooling.Messages;

namespace Razevolution.Tooling
{
    public class MessageQueue
    {
        private ConcurrentQueue<Message> _queue;

        public MessageQueue()
        {
            _queue = new ConcurrentQueue<Message>();
        }

        public void Enqueue(Message message)
        {
            _queue.Enqueue(message);
        }
    }
}
