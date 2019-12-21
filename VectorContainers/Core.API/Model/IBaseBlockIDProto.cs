namespace Core.API.Model
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