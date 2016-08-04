using System;
using System.Collections.Generic;

namespace Razevolution.Tooling.Messages
{
    public class ProjectMessage : Message
    {
        public const string MessageType = "Project";

        public Guid Id { get; set; }

        public List<string> References { get; set; }
    }
}
