using System;
using Raven.Client.Documents;

namespace Core.API.Model
{
    public interface IUnitOfWork
    {
        IDocumentStore Document { get; }

        IBlockGraphRepository BlockGraph { get; }
        IBlockIDRepository BlockID { get; }
        IMessageRepository Message { get; }
        IJobRepository Job { get; }
        IInterpretedRepository Interpreted { get; }

    }
}
