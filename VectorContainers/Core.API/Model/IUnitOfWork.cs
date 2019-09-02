using System;

namespace Core.API.Model
{
    public interface IUnitOfWork : IDisposable
    {
        IMessageRepository Message { get; }
        IMessageLinkRepository MessageLink { get; }
        IMemPoolRepository MemPool { get; }
        IBlockIDRepository BlockID { get; }
        INotIncludedRepository NotIncluded { get; }
    }
}
