using System.Collections.Generic;
using System.Threading.Tasks;
using LightningDB;

namespace Core.API.Model
{
    public interface IRepository<TEntity> where TEntity : class
    {
        Task<List<TEntity>> All();
        Task<TEntity> Get(byte[] key);
        Task<IEnumerable<TEntity>> GetMultiple(byte[] key);
        Task<bool> Delete(byte[] key);
        Task<bool> Put(byte[] key, TEntity entity);
        Task<bool> PutMultiple(byte[] key, IEnumerable<TEntity> entities);
        Task<IEnumerable<TEntity>> Search(byte[] key);
        Task<TEntity> Last();
        Task<IEnumerable<TEntity>> GetRange(int skip, int take);
        Task<IEnumerable<IEnumerable<TEntity>>> GetRangeMultiple(int skip, int take);
        long Count();
    }
}
