using System.IO;
using Newtonsoft.Json.Linq;
using Razevolution.Tooling.Messages;

namespace Razevolution.Tooling
{
    public class DefaultMessageReader : MessageReader
    {
        public DefaultMessageReader(BinaryReader reader, MessageQueue queue) 
            : base(reader, queue)
        {
        }

        protected override Message DeserializeBody(string type, JObject body)
        {
            switch (type)
            {
                case VersionMessage.MessageType:
                    return body.ToObject<VersionMessage>();
                case ProjectMessage.MessageType:
                    return body.ToObject<ProjectMessage>();
                case MetadataMessage.MessageType:
                    return body.ToObject<MetadataMessage>();
                default:
                    return new UnknownMessage()
                    {
                        Body = body.ToString(),
                        Type = type,
                    };
            }
        }
    }
}
