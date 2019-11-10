using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Core.API.Model
{
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        private readonly IDbContext dbContext;
        private readonly ILogger logger;

        public Repository(IDbContext dbContext, ILogger logger)
        {
            this.dbContext = dbContext;
            this.logger = logger;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="value"></param>
        /// <param name="Id"></param>
        /// <returns></returns>
        public Task<TValue> StoreOrUpdate<TValue>(TValue value, string Id = null)
        {
            try
            {
                using (var session = dbContext.Document.OpenSession())
                {
                    if (string.IsNullOrEmpty(Id))
                    {
                        session.Store(value);
                    }
                    else
                    {
                        session.Store(value, null, Id);
                    }

                    session.SaveChanges();
                }

                return Task.FromResult(value);
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< Repository.StoreOrUpdate >>>: {ex.ToString()}");
            }

            return Task.FromResult<TValue>(default);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<TValue> LoadAll<TValue>()
        {
            IEnumerable<TValue> values = null;

            try
            {
                using var session = dbContext.Document.OpenSession();
                values = session.Query<TValue>().ToList();
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< Repository.LoadAll >>>: {ex.ToString()}");
            }

            for (int i = 0, valuesCount = values.Count(); i < valuesCount; i++)
            {
                yield return values.ElementAt(i);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Task<TEntity> Load(string id)
        {
            TEntity entity = default;

            try
            {
                using var session = dbContext.Document.OpenSession();
                entity = session.Load<TEntity>(id);
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< Repository.Load >>>: {ex.ToString()}");
            }

            return Task.FromResult(entity);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Task<bool> Delete(string id)
        {
            bool result = false;

            try
            {
                using var session = dbContext.Document.OpenSession();

                var entity = session.Load<TEntity>(id);

                session.Delete(entity);
                session.SaveChanges();

                result = true;
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< Repository.Delete >>>: {ex.ToString()}");
            }

            return Task.FromResult(result);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public Task<TEntity> GetFirstOrDefault(Expression<Func<TEntity, bool>> expression)
        {
            TEntity entity = default;

            try
            {
                using var session = dbContext.Document.OpenSession();
                entity = session.Query<TEntity>().FirstOrDefault(expression);
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< Repository.GetFirstOrDefault >>>: {ex.ToString()}");
            }

            return Task.FromResult(entity);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public Task<IEnumerable<TEntity>> GetWhere(Expression<Func<TEntity, bool>> expression)
        {
            var entities = Enumerable.Empty<TEntity>();

            try
            {
                using var session = dbContext.Document.OpenSession();
                entities = session.Query<TEntity>().Where(expression).ToList();
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< Repository.GetWhere >>>: {ex.ToString()}");
            }

            return Task.FromResult(entities);
        }
    }
}