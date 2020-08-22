// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Threading.Tasks;
using TGMCore.Model;

namespace TGMCore.Services
{
    public interface IBlockGraphService<TAttach>
    {
        Task<BaseGraphProto<TAttach>> SetBlockGraph(BaseGraphProto<TAttach> blockGraph);
        Task<bool> HasKeyImage(byte[] hash);
    }
}
