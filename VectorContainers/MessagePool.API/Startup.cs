using System;
using System.Diagnostics;
using System.Net.Http;
using Core.API.Broadcast;
using Core.API.Membership;
using Core.API.Model;
using Core.API.Onion;
using MessagePool.API.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MessagePool.API
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            AppDomain.CurrentDomain.FirstChanceException += (sender, eventArgs) =>
            {
                Debug.WriteLine(eventArgs.Exception.ToString());
            };
        }

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
                    Title = "Tangram MessagePool HTTP API",
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

            services.AddTransient<IBroadcastClient, BroadcastClient>();

            services.AddHttpClient<ITorClient, TorClient>()
                    .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                    .AddPolicyHandler((services, request) =>
                    {
                        var logger = services.GetService<ILogger<Startup>>();

                        if (request.Method == HttpMethod.Get)
                        {
                            return Core.API.Helper.PollyEx.GetRetryPolicyAsync(logger);
                        }

                        if (request.Method == HttpMethod.Post)
                        {
                            return Core.API.Helper.PollyEx.GetNoOpPolicyAsync();
                        }

                        return Core.API.Helper.PollyEx.GetRetryPolicy();
                    });


            services.AddSingleton<IDbContext, DbContext>();
            services.AddSingleton<IUnitOfWork, UnitOfWork>();

            services.AddSingleton<IOnionServiceClientConfiguration, OnionServiceClientConfiguration>();
            services.AddHttpClient<IOnionServiceClient, OnionServiceClient>();

            services.AddTransient<IMembershipServiceClient, MembershipServiceClient>();
            services.AddTransient<IMessagePoolService, MessagePoolService>();

            services.AddOptions();
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
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
                   c.SwaggerEndpoint($"{ (!string.IsNullOrEmpty(pathBase) ? pathBase : string.Empty) }/swagger/v1/swagger.json", "MessagePool.API V1");
                   c.OAuthClientId("messagepoolswaggerui");
                   c.OAuthAppName("Message Pool Swagger UI");
               });
        }
    }
}
