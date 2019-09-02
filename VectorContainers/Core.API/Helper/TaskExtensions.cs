using System;
using System.Threading.Tasks;

namespace Core.API.Helper
{
    public static class TaskExtensions
    {
        public static void SwallowException(this Task task)
        {
            task.ContinueWith(_ => { return; });
        }
    }
}
