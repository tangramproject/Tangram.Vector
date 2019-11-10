using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.API.Consensus;
using Core.API.Model;
using Microsoft.Extensions.Logging;

namespace Coin.API.Services
{
    public class JobScrappingService
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly ILogger logger;

        public JobScrappingService(IUnitOfWork unitOfWork, ILogger<JobScrappingService> logger)
        {
            this.unitOfWork = unitOfWork;
            this.logger = logger;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blocks"></param>
        public void Scrape(IEnumerable<BlockID> blocks)
        {
            _ = Task.Factory.StartNew(async () =>
            {
                foreach (var block in blocks)
                {
                    try
                    {
                        var jobProto = await unitOfWork.Job.GetFirstOrDefault(x => x.Hash.Equals(block.Hash) && x.Status != JobState.Polished);
                        if (jobProto == null)
                        {
                            continue;
                        }

                        var success = await unitOfWork.Job.Delete(jobProto.Id);
                        if (!success)
                        {
                            logger.LogError($"<<< JobScrappingService.Scrape >>>: Could not delete job {jobProto.Hash}");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        logger.LogError($"<<< JobScrappingService.Scrape >>>: {ex.ToString()}");
                    }
                }
            });
        }
    }
}
