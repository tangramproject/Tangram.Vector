// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using Microsoft.AspNetCore.DataProtection.Repositories;
using Raven.Client.Documents;

namespace TGMCore.Model
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
