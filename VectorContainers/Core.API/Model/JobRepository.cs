using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Core.API.Model
{
    public class JobRepository<TAttach> : Repository<JobProto<TAttach>>, IJobRepository<TAttach>
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
        public Task Include(JobProto<TAttach> job)
        {
            if (job == null)
                throw new ArgumentNullException(nameof(job));

            try
            {
                var session = dbContext.Document.OpenSession();

                foreach (var nNext in job.Model.Deps)
                {
                    var blockGraph = session.Load<BaseGraphProto<TAttach>>(nNext.Id);
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
        public Task<bool> SetState(JobProto<TAttach> job, JobState state)
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hashes"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public async Task SetStates(IEnumerable<string> hashes, JobState state)
        {
            if (hashes == null)
                throw new ArgumentNullException(nameof(hashes));

            try
            {
                if (hashes.Any() != true)
                    return;

                foreach (var next in hashes)
                {
                    var jobProto = await GetFirstOrDefault(x => x.Hash.Equals(next));
                    if (jobProto != null)
                    {
                        jobProto.Status = state;

                        var saved = await StoreOrUpdate(jobProto, jobProto.Id);
                        if (saved == null)
                            throw new Exception($"Could not update job {jobProto.Id}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"<<< BoostGraphActor.MarkAs >>>: {ex.ToString()}");
            }
        }
    }
}
