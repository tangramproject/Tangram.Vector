// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.Threading.Tasks;

namespace TGMCore.Extentions
{
    public static class TaskExtensions
    {
        public static void SwallowException(this Task task)
        {
            task.ContinueWith(_ => { return; });
        }
    }
}
