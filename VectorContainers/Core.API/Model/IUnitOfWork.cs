using Microsoft.AspNetCore.DataProtection.Repositories;
using Raven.Client.Documents;

namespace Core.API.Model
{
    public interface IUnitOfWork
    {
        IDocumentStore Document { get; }
        IMessageRepository Message { get; }
        IXmlRepository DataProtectionKeys { get; }
        IDataProtectionPayloadReposittory DataProtectionPayload { get; }
        IBaseBlockIDRepository<TAttach> CreateBaseBlockIDOf<TAttach>();
        IBaseGraphRepository<TAttach> CreateBaseGraphOf<TAttach>();
        IJobRepository<TAttach> CreateJobOf<TAttach>();
    }
}
