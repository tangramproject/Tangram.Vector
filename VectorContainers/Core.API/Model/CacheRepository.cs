using System;
using Microsoft.Extensions.Logging;

namespace Core.API.Model
{
    public class CacheRepository : Repository<BlockGraphProto>, ICacheRepository
    {
        private readonly IDbContext dbContext;
        private readonly ILogger logger;

        public CacheRepository(IDbContext dbContext, ILogger logger)
            : base(dbContext, logger)
        {
            this.dbContext = dbContext;
            this.logger = logger;
        }
    }
}
