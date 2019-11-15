using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<FailureDetectionService> _logger;

        public FailureDetectionService(ILogger<FailureDetectionService> logger, FailureDetectionProvider failureDetectionProvider)
        {
            _logger = logger;
            _failureDetectionProvider = failureDetectionProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"{nameof(FailureDetectionService)} running");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation($"Starting next Protocol Period");

                    await Task.Run(() =>
                    {
                        _failureDetectionProvider.Start();
                    }, stoppingToken);

                    await Task.Delay(TimeSpan.FromMilliseconds(7000), stoppingToken);

                    await Task.Run(() =>
                    {
                        _failureDetectionProvider.ProtocolPeriodExpired();
                    }, stoppingToken);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"{nameof(FailureDetectionService)} exception");
                }
            }
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{nameof(FailureDetectionService)} started");

            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{nameof(FailureDetectionService)} stopped");

            return base.StopAsync(cancellationToken);
        }

        public override void Dispose()
        {
            _logger.LogInformation($"{nameof(FailureDetectionService)} disposed");

            base.Dispose();
        }
    }
}



