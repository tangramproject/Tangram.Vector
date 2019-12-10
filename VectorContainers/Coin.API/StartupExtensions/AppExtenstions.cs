using Coin.API.Services;
using Core.API.Providers;
using Core.API.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Coin.API.StartupExtensions
{
    public static class AppExtenstions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddCoinService(this IServiceCollection services)
        {
            services.AddTransient<ICoinService, CoinService>();
            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddMissingBlocksProvider<TAttach>(this IServiceCollection services)
        {
            services.AddSingleton<MissingBlocksProvider<TAttach>>();
            services.AddHostedService<MissingBlocksService<TAttach>>();
            return services;
        }
    }
}
