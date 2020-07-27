// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Threading.Tasks;
using TGMCore.Messages;

namespace TGMCore.Providers
{
    public interface ISipActorProvider
    {
        void Register(HashedMessage message);
        Task<bool> GracefulStop(GracefulStopMessge messge);
    }
}
