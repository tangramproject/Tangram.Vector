using System.Threading.Tasks;

namespace Core.API.Model
{
    public interface IJobRepository: IRepository<JobProto>
    {
        Task Include(JobProto job);
        Task<bool> SetStatus(JobProto job, JobState state);
    }
}
