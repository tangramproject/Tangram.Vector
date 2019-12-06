using Microsoft.Extensions.Logging;
using Raven.Client.Documents;

namespace Core.API.Model
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IDbContext dbContext;
        private readonly ILogger logger;

        public IMessageRepository Message { get; private set; }
        public IDocumentStore Document { get; }

        public UnitOfWork(IDbContext dbContext, ILogger<UnitOfWork> logger)
        {
            this.dbContext = dbContext;
            this.logger = logger;

            Document = dbContext.Document;
            Message = new MessageRepository(dbContext, logger);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TAttach"></typeparam>
        /// <returns></returns>
        public IBaseGraphRepository<TAttach> CreateBaseGraphOf<TAttach>()
        {
            return new BaseGraphRepository<TAttach>(dbContext, logger);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TAttach"></typeparam>
        /// <returns></returns>
        public IJobRepository<TAttach> CreateJobOf<TAttach>()
        {
            return new JobRepository<TAttach>(dbContext, logger);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TAttach"></typeparam>
        /// <returns></returns>
        public IBaseBlockIDRepository<TAttach> CreateBaseBlockIDOf<TAttach>()
        {
            return new BaseBlockIDRepository<TAttach>(dbContext, logger);
        }
    }
}
