// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations;
using Raven.Client.Exceptions;
using Raven.Client.Exceptions.Database;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;

namespace TGMCore.Model
{
    public sealed class DbContext : IDbContext, IDisposable
    {
        private readonly IConfiguration configuration;
        private readonly ILogger logger;

        public IDocumentStore Document { get; private set; }

        public DbContext(IConfiguration configuration, ILogger<DbContext> logger)
        {
            this.configuration = configuration;
            this.logger = logger;

            if (Document == null)
            {
                Document = new DocumentStore
                {
                    Urls = new[] { configuration["Database:url"] },
                    Conventions =
                {
                    MaxNumberOfRequestsPerSession = 30,
                    UseOptimisticConcurrency = true
                },
                    Database = "Tangram",
                }.Initialize();

                EnsureDatabaseExists(Document, "Tangram");
            }
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
                using (var session = Document.OpenSession())
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
                logger.LogError($"<<< DbContext.StoreOrUpdate >>>: {ex}");
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
                using var session = Document.OpenSession();
                values = session.Query<TValue>().ToList();
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< DbContext.LoadAll >>>: {ex}");
            }

            for (int i = 0, valuesCount = values.Count(); i < valuesCount; i++)
            {
                yield return values.ElementAt(i);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="store"></param>
        /// <param name="database"></param>
        /// <param name="createDatabaseIfNotExists"></param>
        private void EnsureDatabaseExists(IDocumentStore store, string database = null, bool createDatabaseIfNotExists = true)
        {
            database ??= store.Database;

            if (string.IsNullOrWhiteSpace(database))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(database));

            try
            {
                store.Maintenance.ForDatabase(database).Send(new GetStatisticsOperation());
            }
            catch (DatabaseDoesNotExistException)
            {
                if (createDatabaseIfNotExists == false)
                    throw;

                try
                {
                    store.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord(database)));
                }
                catch (ConcurrencyException)
                {
                    // Database already exists
                }
            }
        }

        public void Dispose()
        {
            if (Document != null)
            {
                Document.Dispose();
            }
        }
    }
}


