namespace Core.API.Messages
{
    public class HashedMessage
    {
        public byte[] Hash { get; }

        public HashedMessage(byte[] hash)
        {
            Hash = hash;
        }
    }
}
