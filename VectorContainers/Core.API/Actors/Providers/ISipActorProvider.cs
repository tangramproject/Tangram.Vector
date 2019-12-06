using System;
using System.Threading.Tasks;
using Core.API.Messages;

namespace Core.API.Actors.Providers
{
    public interface ISipActorProvider
    {
        void Register(HashedMessage message);
        Task<bool> GracefulStop(GracefulStopMessge messge);
    }
}
