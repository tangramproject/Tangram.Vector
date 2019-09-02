using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using LightningDB;
using System.Text;
using Core.API.Helper;
using Microsoft.Extensions.Logging;

namespace Core.API.Model
{
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        private static readonly AsyncLock putMutex = new AsyncLock();
        private static readonly AsyncLock deleteMutex = new AsyncLock();

        private readonly ILogger logger;

        public string Name { get; }

        public Repository(string name, string filePath, ILogger logger)
        {
            Name = name;
            DbContext.Instance.SetEngine(name, filePath);
            this.logger = logger;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Task<List<TEntity>> All()
        {
            var result = new List<TEntity>();

            try
            {
                using (var tx = DbContext.Instance.LightningEnvironment.BeginTransaction(TransactionBeginFlags.ReadOnly))
                using (var db = tx.OpenDatabase(Name))
                using (var cur = tx.CreateCursor(db))
                {
                    while (cur.MoveNext())
                    {
                        var element = Util.DeserializeProto<TEntity>(cur.Current.Value);
                        if (element != null)
                        {
                            result.Add(element);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }

            return Task.FromResult(result);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Task<TEntity> Get(byte[] key)
        {
            TEntity entity = default;

            try
            {
                using (var tx = DbContext.Instance.LightningEnvironment.BeginTransaction(TransactionBeginFlags.ReadOnly))
                using (var db = tx.OpenDatabase(Name))
                {
                    tx.TryGet(db, key, out byte[] result);
                    entity = Util.DeserializeProto<TEntity>(result);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }

            return Task.FromResult(entity);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Task<IEnumerable<TEntity>> GetMultiple(byte[] key)
        {
            IEnumerable<TEntity> entity = null;

            try
            {
                using (var tx = DbContext.Instance.LightningEnvironment.BeginTransaction(TransactionBeginFlags.ReadOnly))
                using (var db = tx.OpenDatabase(Name))
                {
                    tx.TryGet(db, key, out byte[] result);
                    entity = Util.DeserializeListProto<TEntity>(result);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }

            return Task.FromResult(entity);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<bool> Delete(byte[] key)
        {
            bool result = false;

            using (await deleteMutex.LockAsync())
            {
                try
                {
                    using (var tx = DbContext.Instance.LightningEnvironment.BeginTransaction())
                    using (var db = tx.OpenDatabase(Name, new DatabaseConfiguration { Flags = DatabaseOpenFlags.Create }))
                    {
                        tx.Delete(db, key);

                        result = true;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                }
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public async Task<bool> Put(byte[] key, TEntity entity)
        {
            bool result = false;

            using (await putMutex.LockAsync())
            {
                try
                {
                    using (var tx = DbContext.Instance.LightningEnvironment.BeginTransaction())
                    using (var db = tx.OpenDatabase(Name, new DatabaseConfiguration { Flags = DatabaseOpenFlags.Create }))
                    {
                        var data = Util.SerializeProto(entity);

                        tx.Put(db, key, data);
                        tx.Commit();

                        result = true;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                }

                return result;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public async Task<bool> PutMultiple(byte[] key, IEnumerable<TEntity> entities)
        {
            bool result = false;

            using (await putMutex.LockAsync())
            {
                try
                {
                    using (var tx = DbContext.Instance.LightningEnvironment.BeginTransaction())
                    using (var db = tx.OpenDatabase(Name, new DatabaseConfiguration { Flags = DatabaseOpenFlags.Create }))
                    {
                        var data = Util.SerializeProto(entities);

                        tx.Put(db, key, data);
                        tx.Commit();

                        result = true;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                }

                return result;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Task<IEnumerable<TEntity>> Search(byte[] key)
        {
            List<TEntity> list = null;

            try
            {
                using (var tx = DbContext.Instance.LightningEnvironment.BeginTransaction(TransactionBeginFlags.ReadOnly))
                using (var db = tx.OpenDatabase(Name))
                using (var cur = tx.CreateCursor(db))
                {
                    var results = cur.Where(x => Encoding.UTF8.GetString(x.Key).StartsWith(key.ToStr(), StringComparison.CurrentCulture));
                    if (results != null)
                    {
                        list = results.Select(item => Util.DeserializeProto<TEntity>(item.Value)).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }

            return Task.FromResult(list.AsEnumerable());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Task<TEntity> Last()
        {
            TEntity entity = default;
            try
            {
                using (var tx = DbContext.Instance.LightningEnvironment.BeginTransaction(TransactionBeginFlags.ReadOnly))
                using (var db = tx.OpenDatabase(Name))
                using (var cur = tx.CreateCursor(db))
                {
                    entity = Util.DeserializeProto<TEntity>(cur.Current.Value);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
            return Task.FromResult(entity);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="take"></param>
        /// <param name="skip"></param>
        /// <returns></returns>
        public Task<IEnumerable<TEntity>> GetRange(int skip, int take)
        {
            List<TEntity> list = null;

            try
            {
                using (var tx = DbContext.Instance.LightningEnvironment.BeginTransaction(TransactionBeginFlags.ReadOnly))
                using (var db = tx.OpenDatabase(Name))
                using (var cur = tx.CreateCursor(db))
                {
                    var results = cur.Select(x => x).Skip(skip).Take(take);
                    if (results != null)
                    {
                        list = results.Select(item => Util.DeserializeProto<TEntity>(item.Value)).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }

            return Task.FromResult(list.AsEnumerable());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        public Task<IEnumerable<IEnumerable<TEntity>>>GetRangeMultiple(int skip, int take)
        {
            List<IEnumerable<TEntity>> list = null;

            try
            {
                using (var tx = DbContext.Instance.LightningEnvironment.BeginTransaction(TransactionBeginFlags.ReadOnly))
                using (var db = tx.OpenDatabase(Name))
                using (var cur = tx.CreateCursor(db))
                {
                    var results = cur.Select(x => x).Skip(skip).Take(take);
                    if (results != null)
                    {
                        list = results.Select(item => Util.DeserializeListProto<TEntity>(item.Value)).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }

            return Task.FromResult(list.AsEnumerable());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public long Count()
        {
            long count = 0;

            try
            {
                using (var tx = DbContext.Instance.LightningEnvironment.BeginTransaction())
                using (var db = tx.OpenDatabase(Name))
                {
                    count =  tx.GetEntriesCount(db);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }

            return count;
        }
    }
}
