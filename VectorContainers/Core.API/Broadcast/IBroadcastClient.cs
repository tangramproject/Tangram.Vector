using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Core.API.Broadcast
{
    public interface IBroadcastClient
    {
        Task BroadcastMessageAsync(object message, Uri route);
    }
}
