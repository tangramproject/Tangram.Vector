// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TGMCore.Model
{
    public class MessageRepository : Repository<MessageProto>, IMessageRepository
    {
        private readonly IDbContext dbContext;
        private readonly ILogger logger;

        public MessageRepository(IDbContext dbContext, ILogger logger)
            : base(dbContext, logger)
        {
            this.dbContext = dbContext;
            this.logger = logger;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Task<int> Count(string hash)
        {
            int count = 0;

            try
            {
                using var session = dbContext.Document.OpenSession();

                count = session.Query<MessageProto>().Where(x => x.Address.Equals(hash)).Count();
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< MessageRepository.Count >>>: {ex.ToString()}");
            }

            return Task.FromResult(count);
        }
    }
}
