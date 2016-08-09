using System.Collections.Concurrent;
using Razevolution.Tooling.Messages;

namespace Razevolution.Tooling
{
    public class MessageQueue : BlockingCollection<Message>
    {
    }
}
