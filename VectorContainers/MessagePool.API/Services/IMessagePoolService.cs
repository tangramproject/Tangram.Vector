using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.API.Model;

namespace MessagePool.API.Services
{
    public interface IMessagePoolService
    {
        Task<MessageProto> AddMessage(MessageProto message);
        Task<List<MessageProto>> GetMessages(string address, int skip, int take);
        Task<int> Count(string address);
    }
}
