using System;
using Microsoft.Extensions.Logging;

namespace Core.API.Model
{
    public class StampRepository : Repository<StampProto>, IStampRepository
    {
        private readonly IDbContext dbContext;
        private readonly ILogger logger;

        public StampRepository(IDbContext dbContext, ILogger logger)
            : base(dbContext, logger)
        {
            this.dbContext = dbContext;
            this.logger = logger;
        }
    }
}
