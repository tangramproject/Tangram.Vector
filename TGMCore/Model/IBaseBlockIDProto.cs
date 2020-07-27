// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

namespace TGMCore.Model
{
    public interface IBaseBlockIDProto<TAttach>
    {
        string Hash { get; set; }
        ulong Node { get; set; }
        ulong Round { get; set; }
        BaseBlockProto<TAttach> SignedBlock { get; set; }
        string PreviousHash { get; set; }

        string ToString();
    }
}