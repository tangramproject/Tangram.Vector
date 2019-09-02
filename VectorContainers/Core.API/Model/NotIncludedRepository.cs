using System;
using Microsoft.Extensions.Logging;

namespace Core.API.Model
{
    public class NotIncludedRepository : Repository<BlockGraphProto>, INotIncludedRepository
    {
        public NotIncludedRepository(string name, string filePath, ILogger logger)
            : base(name, filePath, logger) { }
    }
}
