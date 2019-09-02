using System;

namespace Core.API.Model
{
    public interface IMessageRepository: IRepository<MessageProto>
    {
    }

    public interface IMessageLinkRepository : IRepository<MessageProtoList>
    {
    }
}
