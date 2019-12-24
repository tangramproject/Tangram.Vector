using System.Threading.Tasks;
using Core.API.Model;
using libsignal.ecc;

namespace Core.API.POS
{
    public interface ILotteryService
    {
        ECKeyPair GenerateKeyPair();
        string[] PickRandomParticipants(string[] participants, byte[] seed);
        Task<LotteryWinnerProto> PickWinner();
        Task<bool> VerifyWinner(LotteryWinnerProto lotteryWinner);
    }
}