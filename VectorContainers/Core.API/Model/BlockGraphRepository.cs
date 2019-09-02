using Microsoft.Extensions.Logging;

namespace Core.API.Model
{
    public class BlockGraphRepository : Repository<BlockGraphProto>, IBlockGraphRepository
    {
        public BlockGraphRepository(string name, string filePath, ILogger logger)
            : base(name, filePath, logger) { }
    }
}
