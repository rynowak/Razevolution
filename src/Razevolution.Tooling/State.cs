using System;
using System.Collections.Generic;

namespace Razevolution.Tooling
{
    public class State
    {
        public Dictionary<Guid, Project> Projects { get; } = new Dictionary<Guid, Project>();

        public class Project
        {
            public List<string> Documents { get; set; }

            public Guid Id { get; set; }

            public byte[] Metadata { get; set; }

            public string Name { get; set; }

            public string Path { get; set; }

            public List<string> References { get; set; }

            public string Root { get; set; }
        }
    }
}