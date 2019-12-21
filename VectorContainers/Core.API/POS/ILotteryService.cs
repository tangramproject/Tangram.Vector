using System.Threading.Tasks;
using Core.API.Model;
using libsignal.ecc;

namespace Core.API.POS
{
    public interface ILotteryService
    {
        ECKeyPair GenerateKeyPair();
        ulong[] PickRandomParticipants(ulong[] participants, byte[] seed);
        Task<LotteryWinnerProto> PickWinner();
        Task<bool> VerifyWinner(LotteryWinnerProto lotteryWinner);
    }
}