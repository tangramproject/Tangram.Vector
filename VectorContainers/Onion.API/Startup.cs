using System;
using Akka.Actor;
using Core.API.Onion;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Onion.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddResponseCompression();

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
            services.AddMvc(option => option.EnableEndpointRouting = false);

            services.AddControllers();

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    License = new Microsoft.OpenApi.Models.OpenApiLicense
                    {
                        Name = "Attribution-NonCommercial-NoDerivatives 4.0 International",
                        Url = new Uri("https://raw.githubusercontent.com/tangramproject/Tangram.Vector/initial/LICENSE")
                    },
                    Title = "Tangram Onion HTTP API",
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

            services.AddHttpContextAccessor();

            services.AddOptions();

            services.AddSingleton<ITorProcessService, TorProcessService>();
            services.AddHostedService<TorProcessService>();
        }

        public void Configure(IApplicationBuilder app)
        {
            var pathBase = Configuration["PATH_BASE"];
            if (!string.IsNullOrEmpty(pathBase))
            {
                app.UsePathBase(pathBase);
            }

            app.UseStaticFiles();
            app.UseRouting();
            app.UseCors("default");
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseSwagger()
               .UseSwaggerUI(c =>
               {
                   c.SwaggerEndpoint($"{ (!string.IsNullOrEmpty(pathBase) ? pathBase : string.Empty) }/swagger/v1/swagger.json", "Onion.API V1");
                   c.OAuthClientId("onionswaggerui");
                   c.OAuthAppName("Onion Swagger UI");
               });
        }
    }
}
