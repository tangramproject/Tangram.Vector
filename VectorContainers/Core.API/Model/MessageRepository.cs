using System;
using Microsoft.Extensions.Logging;

namespace Core.API.Model
{
    public class MessageRepository : Repository<MessageProto>, IMessageRepository
    {
        public MessageRepository(string name, string filePath, ILogger logger)
            : base(name, filePath, logger) { }
    }

    public class MessageLinkRepository : Repository<MessageProtoList>, IMessageLinkRepository
    {
        public MessageLinkRepository(string name, string filePath, ILogger logger)
            : base(name, filePath, logger) { }
    }
}
