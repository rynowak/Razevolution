using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Razevolution.Tooling
{
    public class State
    {
        public Dictionary<Guid, Project> Projects { get; } = new Dictionary<Guid, Project>();

        public class Project
        {
            public List<string> Documents { get; } = new List<string>();

            public Guid Id { get; set; }

            public byte[] Metadata { get; set; }

            public string Name { get; set; }

            public string Path { get; set; }

            public Dictionary<string, MetadataReference> References { get; } = new Dictionary<string, MetadataReference>();

            public string Root { get; set; }
        }
    }
}