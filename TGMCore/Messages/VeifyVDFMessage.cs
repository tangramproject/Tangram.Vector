// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

namespace TGMCore.Messages
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
