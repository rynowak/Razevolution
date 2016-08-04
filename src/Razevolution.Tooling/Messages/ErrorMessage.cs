
using System;

namespace Razevolution.Tooling.Messages
{
    public class ErrorMessage : Message
    {
        public Exception Exception { get; set; }

        public string OriginalText { get; set; }
    }
}
