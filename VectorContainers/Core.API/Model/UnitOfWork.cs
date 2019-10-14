
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;

namespace Core.API.Model
{
    public class UnitOfWork : IUnitOfWork
    {
        public IBlockGraphRepository BlockGraph { get; private set; }
        public IBlockIDRepository BlockID { get; private set; }
        public IMessageRepository Message { get; private set; }
        public IJobRepository Job { get; private set; }
        public IInterpretedRepository Interpreted { get; private set; }

        public IDocumentStore Document { get; }

        public UnitOfWork(IDbContext dbContext, ILogger<UnitOfWork> logger)
        {
            Document = dbContext.Document;

            BlockGraph = new BlockGraphRepository(dbContext, logger);
            BlockID = new BlockIDRepository(dbContext, logger);
            Message = new MessageRepository(dbContext, logger);
            Job = new JobRepository(dbContext, logger);
            Interpreted = new InterpretedRepository(dbContext, logger);
        }
    }
}
