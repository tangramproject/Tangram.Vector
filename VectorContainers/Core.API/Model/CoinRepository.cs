using System.Text;
using Microsoft.Extensions.Logging;

namespace Core.API.Model
{
    public class CoinRepository : Repository<BlockGraphProto>, ICoinRepository
    {
        public CoinRepository(string name, string filePath, ILogger logger)
            : base(name, filePath, logger) { }
    }

    public class MemPoolRepository : Repository<BlockGraphProto>, IMemPoolRepository
    {
        public MemPoolRepository(string name, string filePath, ILogger logger)
            : base(name, filePath, logger) { }
    }
}
