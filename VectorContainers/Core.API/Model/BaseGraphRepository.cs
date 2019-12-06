using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Core.API.Model
{
    public class BaseGraphRepository<TAttach> : Repository<BaseGraphProto<TAttach>>, IBaseGraphRepository<TAttach>
    {
        private readonly IDbContext dbContext;
        private readonly ILogger logger;

        public BaseGraphRepository(IDbContext dbContext, ILogger logger)
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
        public async Task<List<BaseGraphProto<TAttach>>> More(IEnumerable<BaseGraphProto<TAttach>> blocks)
        {
            var hasMoreBlocks = new List<BaseGraphProto<TAttach>>();

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
        public Task Include(IEnumerable<BaseGraphProto<TAttach>> blockGraphs, ulong node)
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
        /// <param name="blockGraph"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        public async Task<BaseGraphProto<TAttach>> CanAdd(BaseGraphProto<TAttach> blockGraph, ulong node)
        {
            var blockGraphs = await GetWhere(x => x.Block.Hash.Equals(blockGraph.Block.Hash) && x.Block.Node.Equals(node));
            if (blockGraphs.Any())
            {
                var graph = blockGraphs.FirstOrDefault(x => x.Block.Round.Equals(blockGraph.Block.Round));
                if (graph != null)
                {
                    logger.LogWarning($"<<< BlockGraphRepository.CanAdd >>>: Block exists for node {graph.Block.Node} and round {graph.Block.Round}");
                    return null;
                }
            }

            return blockGraph;
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

                count = session.Query<BaseGraphProto<TAttach>>()
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
        public Task<BaseGraphProto<TAttach>> GetMax(string hash, ulong node)
        {
            if (string.IsNullOrEmpty(hash))
                throw new ArgumentNullException(nameof(hash));

            var blockGraphs = Enumerable.Empty<BaseGraphProto<TAttach>>();
            BaseGraphProto<TAttach> blockGraph = null;

            try
            {
                using var session = dbContext.Document.OpenSession();

                blockGraphs = session.Query<BaseGraphProto<TAttach>>()
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
        public Task<BaseGraphProto<TAttach>> GetPrevious(ulong node, ulong round)
        {
            if (round < 0)
                throw new ArgumentOutOfRangeException(nameof(round));

            BaseGraphProto<TAttach> blockGraph = null;

            try
            {
                using var session = dbContext.Document.OpenSession();

                round -= 1;
                blockGraph = session.Query<BaseGraphProto<TAttach>>()
                    .FirstOrDefault(x => x.Block.Node.Equals(node) && x.Block.Round.Equals(round));
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< BlockGraphRepository.GetPrevious >>>: {ex.ToString()}");
            }

            return Task.FromResult(blockGraph);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="round"></param>
        /// <returns></returns>
        public Task<BaseGraphProto<TAttach>> GetPrevious(string hash, ulong node, ulong round)
        {
            if (round < 0)
                throw new ArgumentOutOfRangeException(nameof(round));

            BaseGraphProto<TAttach> blockGraph = null;

            try
            {
                using var session = dbContext.Document.OpenSession();

                round -= 1;
                blockGraph = session.Query<BaseGraphProto<TAttach>>()
                    .FirstOrDefault(x => x.Block.Hash.Equals(hash) && x.Block.Node.Equals(node) && x.Block.Round.Equals(round));
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< BlockGraphRepository.GetPrevious >>>: {ex.ToString()}");
            }

            return Task.FromResult(blockGraph);
        }

        /// <summary>
        /// /
        /// </summary>
        /// <typeparam name="P"></typeparam>
        /// <param name="expression"></param>
        /// <returns></returns>
        public Task<IEnumerable<P>> Where<P>(Func<P, bool> expression)
        {
            var entities = Enumerable.Empty<P>();

            try
            {
                using var session = dbContext.Document.OpenSession();
                entities = session.Query<P>().Where(expression).ToList();
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< Repository.GetWhere >>>: {ex.ToString()}");
            }

            return Task.FromResult(entities);
        }
    }
}
