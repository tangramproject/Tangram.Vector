// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

namespace TGMCore.Models
{
    public interface ISignedMessageBase
    {
        byte[] PublicKey { get; set; }
        byte[] Hash { get; set; }
        byte[] Signature { get; set; }
        bool IsValid();
    }
}