using System.Threading.Tasks;

namespace Core.API.POS
{
    public interface ILotteryService
    {
        Task<SignedLotteryTicket> GenerateSignedLotteryTicket(ulong round);
    }
}