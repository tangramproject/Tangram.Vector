using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Core.API.Model
{
    public class BaseBlockIDRepository<TAttach> : Repository<BaseBlockIDProto<TAttach>>, IBaseBlockIDRepository<TAttach>
    {
        private readonly IDbContext dbContext;
        private readonly ILogger logger;

        public BaseBlockIDRepository(IDbContext dbContext, ILogger logger)
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
        public Task<IEnumerable<BaseBlockIDProto<TAttach>>> GetRange(int skip, int take)
        {
            if (skip < 0)
                throw new ArgumentOutOfRangeException(nameof(skip));

            if (take < 0)
                throw new ArgumentOutOfRangeException(nameof(take));

            var blockIds = Enumerable.Empty<BaseBlockIDProto<TAttach>>();

            try
            {
                using var session = dbContext.Document.OpenSession();
                blockIds = session.Query<BaseBlockIDProto<TAttach>>().Skip(skip).Take(take).ToList();
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
        /// <returns></returns>
        public Task<int> Count(ulong node)
        {
            int count = 0;

            try
            {
                using var session = dbContext.Document.OpenSession();

                count = session.Query<BaseBlockIDProto<TAttach>>()
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
