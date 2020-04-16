namespace Core.API.Messages
{
    public class VeifyVDFMessage
    {
        public HeaderMessage Header { get; }
        public string Security { get; }

        public VeifyVDFMessage(HeaderMessage header, string security)
        {
            Header = header;
            Security = security;
        }
    }
}
