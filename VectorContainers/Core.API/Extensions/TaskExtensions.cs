using System.Threading.Tasks;

namespace Core.API.Extentions
{
    public static class TaskExtensions
    {
        public static void SwallowException(this Task task)
        {
            task.ContinueWith(_ => { return; });
        }
    }
}
