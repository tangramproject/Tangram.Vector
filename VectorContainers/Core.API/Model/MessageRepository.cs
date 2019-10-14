using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Core.API.Model
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
        /// <param name="hash"></param>
        /// <returns></returns>
        public Task<IEnumerable<MessageProto>> GetMany(string hash)
        {
            if (string.IsNullOrEmpty(hash))
                throw new ArgumentNullException(nameof(hash));

            var messages = Enumerable.Empty<MessageProto>();

            try
            {
                using var session = dbContext.Document.OpenSession();

                messages = session.Query<MessageProto>().Where(x => x.Address.Equals(hash)).ToList();
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< MessageRepository.GetMany >>>: {ex.ToString()}");
            }

            return Task.FromResult(messages);
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
