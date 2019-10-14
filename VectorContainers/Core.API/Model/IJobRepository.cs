using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.API.Model
{
    public interface IJobRepository: IRepository<JobProto>
    {
        Task<JobProto> Get(string hash);
        Task<IEnumerable<JobProto>> GetStatusMany(JobState state);
        Task Include(JobProto job);
        Task<bool> SetStatus(JobProto job, JobState state);
    }
}
