using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.API.Model
{
    public interface IRepository<TEntity> where TEntity : class
    {
        Task<TValue> StoreOrUpdate<TValue>(TValue value, string Id = null);
        IEnumerable<TValue> LoadAll<TValue>();
        Task<bool> Delete(TEntity entity);
        Task<TEntity> Load(string id);
    }
}
