namespace Razevolution.Tooling.Messages
{
    public class MetadataMessage : Message
    {
        public const string MessageType = "metadata";

        public string Name { get; set; }

        public byte[] Bytes { get; set; }
    }
}
