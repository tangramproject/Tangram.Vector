using Microsoft.Extensions.Logging;

namespace Core.API.Model
{
    public class BlockIDRepository : Repository<BlockIDProto>, IBlockIDRepository
    {
        public BlockIDRepository(string name, string filePath, ILogger logger)
            : base(name, filePath, logger) { }
    }
}
