using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.API.Helper
{
    public class AsyncLock : IDisposable
    {
        private SemaphoreSlim semaphoreSlim;

        public AsyncLock()
        {
            semaphoreSlim = new SemaphoreSlim(1, 1);
        }

        public AsyncLock(SemaphoreSlim semaphoreSlim)
        {
            this.semaphoreSlim = semaphoreSlim;
        }

        public async Task<AsyncLock> LockAsync()
        {
            await semaphoreSlim.WaitAsync();
            return this;
        }

        public void Dispose()
        {
            semaphoreSlim.Release();
        }
    }
}
