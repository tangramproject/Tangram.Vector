using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.API.Model
{
    public interface IMessageRepository: IRepository<MessageProto>
    {
        Task<IEnumerable<MessageProto>> GetMany(string hash);
        Task<int> Count(string hash);
    }
}
