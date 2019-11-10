using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Coin.API.Services;
using System.Diagnostics;
using Coin.API.Middlewares;
using Core.API.Consensus;
using Akka.Actor;
using Microsoft.Extensions.Hosting;
using Coin.API.ActorProviders;
using Coin.API.StartupExtensions;

namespace Coin.API
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configuration"></param>
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            AppDomain.CurrentDomain.FirstChanceException += (sender, eventArgs) =>
            {
                Debug.WriteLine(eventArgs.Exception.ToString());
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddResponseCompression();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
            services.AddMvc(option => option.EnableEndpointRouting = false);
            services.AddControllers();
            services.AddSwaggerGenOptions();
            services.AddHttpContextAccessor();
            services.AddOptions();
            services.Configure<BlockmainiaOptions>(Configuration);
            services.AddSyncProvider();
            services.AddBroadcastProvider();
            //services.AddMissingBlocksProvider();
            services.AddDbContext();
            services.AddUnitOfWork();
            services.AddOnionServiceClientConfiguration();
            services.AddOnionServiceClient();
            services.AddHttpService();
            services.AddHttpClientHandler();
            services.AddBroadcastClient();
            services.AddTorClient();
            services.AddMembershipServiceClient();
            services.AddActorSystem();
            services.AddNetworkActorProvider();
            services.AddSigningActorProvider();
            services.AddInterpretActorProvider();
            services.AddProcessBlockActorProvider();
            services.AddSipActorProvider();
            services.AddBlockGraphService();
            services.AddCoinService();

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <param name="lifetime"></param>
        public void Configure(IApplicationBuilder app, IHostApplicationLifetime lifetime)
        {
            var pathBase = Configuration["PATH_BASE"];
            if (!string.IsNullOrEmpty(pathBase))
            {
                app.UsePathBase(pathBase);
            }

            app.UseSync();
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
                   c.SwaggerEndpoint($"{ (!string.IsNullOrEmpty(pathBase) ? pathBase : string.Empty) }/swagger/v1/swagger.json", "Coin.API V1");
                   c.OAuthClientId("coinswaggerui");
                   c.OAuthAppName("Coin Swagger UI");
               });

            lifetime.ApplicationStarted.Register(() =>
            {
                app.ApplicationServices.GetService<ActorSystem>();
                app.ApplicationServices.GetService<ISipActorProvider>();
            });

            lifetime.ApplicationStopping.Register(() =>
            {
                app.ApplicationServices.GetService<IHttpService>().Dispose();
                app.ApplicationServices.GetService<ActorSystem>().Terminate().Wait();
            });
        }
    }
}
