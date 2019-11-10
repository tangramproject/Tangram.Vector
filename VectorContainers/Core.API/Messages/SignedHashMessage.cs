namespace Core.API.Messages
{
    public class SignedHashMessage
    {
        public byte[] Hash { get; }
        public SignedHashMessage(byte[] hash)
        {
            Hash = hash;
        }
    }
}
