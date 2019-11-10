using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.API.Model
{
    public interface IJobRepository: IRepository<JobProto>
    {
        Task Include(JobProto job);
        Task<bool> SetState(JobProto job, JobState state);
        Task SetStates(IEnumerable<string> hashes, JobState state);
    }
}
