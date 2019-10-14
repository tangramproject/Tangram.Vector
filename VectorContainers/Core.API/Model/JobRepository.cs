using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Core.API.Model
{
    public class JobRepository : Repository<JobProto>, IJobRepository
    {
        private readonly IDbContext dbContext;
        private readonly ILogger logger;

        public JobRepository(IDbContext dbContext, ILogger logger)
            : base(dbContext, logger)
        {
            this.dbContext = dbContext;
            this.logger = logger;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public Task<JobProto> Get(string hash)
        {
            if (string.IsNullOrEmpty(hash))
                throw new ArgumentOutOfRangeException(nameof(hash));

            var jobs = Enumerable.Empty<JobProto>();

            try
            {
                using var session = dbContext.Document.OpenSession();
                jobs = session.Query<JobProto>().Where(x => x.Hash.Equals(hash)).ToList();

            }
            catch (Exception ex)
            {
                logger.LogError($"<<< JobRepository.Get >>>: {ex.ToString()}");
            }

            return Task.FromResult(jobs.FirstOrDefault());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="job"></param>
        /// <returns></returns>
        public Task Include(JobProto job)
        {
            if (job == null)
                throw new ArgumentNullException(nameof(job));

            try
            {
                var session = dbContext.Document.OpenSession();

                foreach (var nNext in job.BlockGraph.Deps)
                {
                    var blockGraph = session.Load<BlockGraphProto>(nNext.Id);
                    blockGraph.Included = true;

                    session.Store(blockGraph, blockGraph.Id);
                }

                session.SaveChanges();
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< JobRepository.Include >>>: {ex.ToString()}");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Task<IEnumerable<JobProto>> GetStatusMany(JobState state)
        {
            var jobs = Enumerable.Empty<JobProto>();

            try
            {
                using var session = dbContext.Document.OpenSession();
                jobs = session.Query<JobProto>()
                    .Where(x => x.Status == state)
                    .ToList();
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< JobRepository.GetStatusMany >>>: {ex.ToString()}");
            }

            return Task.FromResult(jobs);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="job"></param>
        /// <returns></returns>
        public Task<bool> SetStatus(JobProto job, JobState state)
        {
            if (job == null)
                throw new ArgumentNullException(nameof(job));

            bool result = false;
            try
            {
                using var session = dbContext.Document.OpenSession();

                job.Status = state;

                session.Store(job, null, job.Id);
                session.SaveChanges();

                result = true;

            }
            catch (Exception ex)
            {
                logger.LogError($"<<< JobRepository.SetStatus >>>: {ex.ToString()}");
            }

            return Task.FromResult(result);
        }
    }
}
