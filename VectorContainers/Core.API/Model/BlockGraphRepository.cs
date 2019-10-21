using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Core.API.Model
{
    public class BlockGraphRepository : Repository<BlockGraphProto>, IBlockGraphRepository
    {
        private readonly IDbContext dbContext;
        private readonly ILogger logger;

        public BlockGraphRepository(IDbContext dbContext, ILogger logger)
            : base(dbContext, logger)
        {
            this.dbContext = dbContext;
            this.logger = logger;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blocks"></param>
        /// <returns></returns>
        public async Task<List<BlockGraphProto>> More(IEnumerable<BlockGraphProto> blocks)
        {
            var hasMoreBlocks = new List<BlockGraphProto>();

            try
            {
                foreach (var next in blocks)
                {
                    var hasNext = await GetWhere(x => x.Block.Hash.Equals(next.Block.Hash));
                    foreach (var nNext in hasNext)
                    {
                        var included = hasMoreBlocks
                            .FirstOrDefault(x => x.Block.Hash.Equals(nNext.Block.Hash) && x.Block.Node.Equals(nNext.Block.Node) && x.Block.Round.Equals(nNext.Block.Round));

                        if (included != null)
                            continue;

                        hasMoreBlocks.Add(nNext);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< BlockGraphRepository.More >>>: {ex.ToString()}");
            }

            return hasMoreBlocks;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockGraphs"></param>
        /// <returns></returns>
        public Task Include(IEnumerable<BlockGraphProto> blockGraphs, ulong node)
        {
            if (blockGraphs.Any() != true)
            {
                return Task.CompletedTask;
            }

            try
            {
                using var session = dbContext.Document.OpenSession();

                foreach (var next in blockGraphs)
                {
                    if (next.Block.Node.Equals(node))
                    {
                        next.Included = true;
                        session.Store(next, null, next.Id);
                    }
                }

                session.SaveChanges();
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< BlockGraphRepository.Include >>>: {ex.ToString()}");
            }

            return Task.CompletedTask;
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

                count = session.Query<BlockGraphProto>()
                    .Where(x => x.Block.Node.Equals(node))
                    .Count();
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< BlockGraphRepository.Count >>>: {ex.ToString()}");
            }

            return Task.FromResult(count);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public Task<BlockGraphProto> GetMax(string hash, ulong node)
        {
            if (string.IsNullOrEmpty(hash))
                throw new ArgumentNullException(nameof(hash));

            var blockGraphs = Enumerable.Empty<BlockGraphProto>();
            BlockGraphProto blockGraph = null;

            try
            {
                using var session = dbContext.Document.OpenSession();

                blockGraphs = session.Query<BlockGraphProto>()
                    .Where(x => x.Block.Hash.Equals(hash) && x.Block.Node.Equals(node)).ToList();

                if (blockGraphs?.Any() == true)
                {
                    blockGraph = blockGraphs
                        .FirstOrDefault(x => x.Block.Round.Equals(blockGraphs.Max(m => m.Block.Round)));
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< BlockGraphRepository.GetMax >>>: {ex.ToString()}");
            }

            return Task.FromResult(blockGraph);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="round"></param>
        /// <returns></returns>
        public Task<BlockGraphProto> GetPrevious(ulong node, ulong round)
        {
            if (round < 0)
                throw new ArgumentOutOfRangeException(nameof(round));

            BlockGraphProto blockGraph = null;

            try
            {
                using var session = dbContext.Document.OpenSession();

                round -= 1;
                blockGraph = session.Query<BlockGraphProto>()
                    .FirstOrDefault(x => x.Block.Node.Equals(node) && x.Block.Round.Equals(round));
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< BlockGraphRepository.GetPrevious >>>: {ex.ToString()}");
            }

            return Task.FromResult(blockGraph);
        }
    }
}
