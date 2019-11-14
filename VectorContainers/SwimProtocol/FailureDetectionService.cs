using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SwimProtocol
{
    public class FailureDetectionService : BackgroundService
    {
        private readonly FailureDetectionProvider _failureDetectionProvider;

        public FailureDetectionService(FailureDetectionProvider failureDetectionProvider)
        {
            _failureDetectionProvider = failureDetectionProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Run(() =>
            {
                _failureDetectionProvider.Start();
            }, stoppingToken);
        }
    }
}
