using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.API.Helper
{
    public class ScopedAsyncLock : IDisposable
    {
        private bool disposed;
        private SemaphoreSlim semaphoreSlim;

        public ScopedAsyncLock()
        {
            semaphoreSlim = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<AsyncLock> CreateLockAsync()
        {
            var scopedLock = new AsyncLock(semaphoreSlim);
            await scopedLock.LockAsync();
            return scopedLock;
        }

        #region IDisposable stuff

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed && disposing && semaphoreSlim != null)
            {
                semaphoreSlim.Dispose();
                semaphoreSlim = null;
            }

            disposed = true;
        }


        #endregion IDisposable stuff
    }
}
