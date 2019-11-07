using System;
using Microsoft.Extensions.DependencyInjection;

namespace Coin.API.StartupExtensions
{
    public static class SwaggerExtentions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddSwaggerGenOptions(this IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    License = new Microsoft.OpenApi.Models.OpenApiLicense
                    {
                        Name = "Attribution-NonCommercial-NoDerivatives 4.0 International",
                        Url = new Uri("https://raw.githubusercontent.com/tangramproject/Tangram.Vector/initial/LICENSE")
                    },
                    Title = "Tangram Coin HTTP API",
                    Version = "v1",
                    Description = "Backend services.",
                    TermsOfService = new Uri("https://tangrams.io/legal/"),
                    Contact = new Microsoft.OpenApi.Models.OpenApiContact
                    {
                        Email = "dev@getsneak.org",
                        Url = new Uri("https://tangrams.io/about-tangram/team/")
                    }
                });
            });

            return services;
        }
    }
}
