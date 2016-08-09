using System;

namespace Razevolution.Tooling.Messages
{
    public class MetadataMessage : Message
    {
        public const string MessageType = "metadata";

        public string AssemblyName { get; set; }

        public byte[] Bytes { get; set; }

        public Guid Id { get; set; }

        public string ProjectName { get; set; }
    }
}
