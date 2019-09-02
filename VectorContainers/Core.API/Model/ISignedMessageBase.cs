namespace Core.API.Models
{
    public interface ISignedMessageBase
    {
        byte[] PublicKey { get; set; }
        byte[] Hash { get; set; }
        byte[] Signature { get; set; }
        bool IsValid();
    }
}