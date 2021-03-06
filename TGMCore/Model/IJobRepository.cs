﻿// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Collections.Generic;
using System.Threading.Tasks;

namespace TGMCore.Model
{
    public interface IJobRepository<TAttach>: IRepository<JobProto<TAttach>>
    {
        Task Include(JobProto<TAttach> job);
        Task<bool> SetState(JobProto<TAttach> job, JobState state);
        Task SetStates(IEnumerable<string> hashes, JobState state);
    }
}
