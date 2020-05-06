using System;
using Microsoft.Extensions.Logging;

namespace Core.API.Model
{
    public class DataProtectionPayloadReposittory: Repository<DataProtectionPayloadProto>, IDataProtectionPayloadReposittory
    {
        private readonly IDbContext dbContext;
        private readonly ILogger logger;

        public DataProtectionPayloadReposittory(IDbContext dbContext, ILogger logger)
            : base(dbContext, logger)
        {
            this.dbContext = dbContext;
            this.logger = logger;
        }
    }
}
