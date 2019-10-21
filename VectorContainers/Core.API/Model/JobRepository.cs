using System;
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
