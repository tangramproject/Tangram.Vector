// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using Microsoft.Extensions.Logging;

namespace TGMCore.Model
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
