using Core.API.Model;

namespace Core.API.Messages
{
    public class HeaderMessage
    {
        public HeaderProto Proto { get; }

        public HeaderMessage(HeaderProto header)
        {
            Proto = header;
        }
    }
}
