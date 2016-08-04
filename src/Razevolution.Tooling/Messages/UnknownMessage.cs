
namespace Razevolution.Tooling.Messages
{
    public class UnknownMessage : Message
    {
        public const string MessageType = "unknown";

        public string Type { get; set; }

        public string Body { get; set; }
    }
}
