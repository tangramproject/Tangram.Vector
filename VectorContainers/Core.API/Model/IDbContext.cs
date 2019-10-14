using System.Collections.Generic;
using System.Threading.Tasks;
using Raven.Client.Documents;

namespace Core.API.Model
{
    public interface IDbContext
    {
        IDocumentStore Document { get; }
        Task<TValue> StoreOrUpdate<TValue>(TValue value, string Id = null);
        IEnumerable<TValue> LoadAll<TValue>();
    }
}