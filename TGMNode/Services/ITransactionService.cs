// TGMNode by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Threading.Tasks;
using TGMNode.Model;

namespace TGMNode.Services
{
    public interface ITransactionService
    {
        Task<byte[]> AddTransaction(TransactionProto coin);
        Task<byte[]> GetTransaction(string key);
        Task<byte[]> GetTransactions(string key, int skip, int take);
        Task<byte[]> GetTransactions(string key);
        Task<byte[]> GetTransactions(int skip, int take);
    }
}
