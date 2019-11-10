using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.API.Model
{
    public interface IMessageRepository: IRepository<MessageProto>
    {
        Task<int> Count(string hash);
    }
}
