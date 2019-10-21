using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Core.API.Model
{
    public class BlockIDRepository : Repository<BlockIDProto>, IBlockIDRepository
    {
        private readonly IDbContext dbContext;
        private readonly ILogger logger;

        public BlockIDRepository(IDbContext dbContext, ILogger logger)
            : base(dbContext, logger)
        {
            this.dbContext = dbContext;
            this.logger = logger;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        public Task<IEnumerable<BlockIDProto>> GetRange(int skip, int take)
        {
            if (skip < 0)
                throw new ArgumentOutOfRangeException(nameof(skip));

            if (take < 0)
                throw new ArgumentOutOfRangeException(nameof(take));

            var blockIds = Enumerable.Empty<BlockIDProto>();

            try
            {
                using var session = dbContext.Document.OpenSession();
                blockIds = session.Query<BlockIDProto>().Skip(skip).Take(take).ToList();
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< BlockIDRepository.GetRange >>>: {ex.ToString()}");
            }

            return Task.FromResult(blockIds);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public Task<bool> HasCoin(string hash)
        {
            if (string.IsNullOrEmpty(hash))
                throw new ArgumentNullException(nameof(hash));

            bool exists = false;

            try
            {
                using var session = dbContext.Document.OpenSession();

                var coin = session.Query<BlockIDProto>().FirstOrDefault(x => x.SignedBlock.Coin.Commitment.Equals(hash));
                exists = coin != null;
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< BlockIDRepository.HasCoin >>>: {ex.ToString()}");
            }

            return Task.FromResult(exists);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Task<int> Count(ulong node)
        {
            int count = 0;

            try
            {
                using var session = dbContext.Document.OpenSession();

                count = session.Query<BlockIDProto>()
                    .Where(x => x.Node.Equals(node))
                    .Count();
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< BlockGraphRepository.Count >>>: {ex.ToString()}");
            }

            return Task.FromResult(count);
        }
    }
}
