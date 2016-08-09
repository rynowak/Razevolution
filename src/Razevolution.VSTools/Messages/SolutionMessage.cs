using System;
using System.Collections.Generic;

namespace Razevolution.Tooling.Messages
{
    public class SolutionMessage : Message
    {
        public const string MessageType = "solution";

        public List<Project> Projects { get; set; }

        public class Project
        {
            public Guid Id { get; set; }

            public string Name { get; set; }

            public string Path { get; set; }

            public List<string> References { get; set; }
        }
    }
}
