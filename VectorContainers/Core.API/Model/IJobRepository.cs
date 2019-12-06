using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.API.Model
{
    public interface IJobRepository<TAttach>: IRepository<JobProto<TAttach>>
    {
        Task Include(JobProto<TAttach> job);
        Task<bool> SetState(JobProto<TAttach> job, JobState state);
        Task SetStates(IEnumerable<string> hashes, JobState state);
    }
}
