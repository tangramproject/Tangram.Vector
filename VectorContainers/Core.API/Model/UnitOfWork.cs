
using Microsoft.Extensions.Logging;

namespace Core.API.Model
{
    public class UnitOfWork : IUnitOfWork
    {
        public IMessageRepository Message { get; private set; }
        public IMessageLinkRepository MessageLink { get; private set; }
        public IMemPoolRepository MemPool { get; private set; }
        public IBlockIDRepository BlockID { get; private set; }
        public INotIncludedRepository NotIncluded { get; private set; }

        public UnitOfWork(string filePath, ILogger<UnitOfWork> logger)
        {
            MemPool = new MemPoolRepository("MemPool", filePath, logger);
            Message = new MessageRepository("MessagePool", filePath, logger);
            MessageLink = new MessageLinkRepository("MessagePool", filePath, logger);
            BlockID = new BlockIDRepository("BlockID", filePath, logger);
        }

        public void Dispose() => DbContext.Instance.Dispose();
    }
}
