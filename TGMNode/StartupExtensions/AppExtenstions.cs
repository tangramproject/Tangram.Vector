// TGMNode by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using TGMNode.Services;
using Microsoft.Extensions.DependencyInjection;

namespace TGMNode.StartupExtensions
{
    public static class AppExtenstions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddTransactionService(this IServiceCollection services)
        {
            services.AddTransient<ITransactionService, TransactionService>();
            return services;
        }
    }
}
