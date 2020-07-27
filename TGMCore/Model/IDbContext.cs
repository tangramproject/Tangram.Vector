// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Collections.Generic;
using System.Threading.Tasks;
using Raven.Client.Documents;

namespace TGMCore.Model
{
    public interface IDbContext
    {
        IDocumentStore Document { get; }
        Task<TValue> StoreOrUpdate<TValue>(TValue value, string Id = null);
        IEnumerable<TValue> LoadAll<TValue>();
    }
}