
namespace Razevolution.Tooling.Messages
{
    public class VersionMessage : Message
    {
        public const string MessageType = "Version";

        public int Version { get; set; }
    }
}
