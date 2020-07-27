// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TGMCore.Services
{
    //public class SyncService<I> : BackgroundService
    //{
    //    private readonly SyncProvider<I> _syncProvider;
    //    private readonly ILogger _logger;

    //    public SyncService(SyncProvider<I> syncProvider, ILogger<SyncService<I>> logger)
    //    {
    //        _syncProvider = syncProvider;
    //        _logger = logger;
    //    }

    //    /// <summary>
    //    /// 
    //    /// </summary>
    //    /// <param name="stoppingToken"></param>
    //    /// <returns></returns>
    //    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    //    {
    //        try
    //        {
    //            await _syncProvider.SynchronizeCheck();
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError($"<<< SyncService >>>: {ex.ToString()}");
    //        }
    //    }
    //}
}
