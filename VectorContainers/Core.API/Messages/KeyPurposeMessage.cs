using System;
namespace Core.API.Messages
{
    public class KeyPurposeMessage
    {
        public string Purpose { get; }

        public KeyPurposeMessage(string purpose)
        {
            Purpose = purpose;
        }
    }
}
