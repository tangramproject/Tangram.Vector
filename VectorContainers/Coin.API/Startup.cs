using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using Core.API.Consensus;
using Akka.Actor;
using Microsoft.Extensions.Hosting;
using Coin.API.StartupExtensions;
using Core.API.Network;
using Coin.API.Actors;
using Core.API.Middlewares;
using Core.API.Actors.Providers;
using Core.API.Extensions;
using Coin.API.Model;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Core.API.Model;

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
            var gatewaySection = Configuration.GetSection("Gateway");
            var brokerSection = Configuration.GetSection("Broker");

            services.AddResponseCompression();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
            services.AddMvc(option => option.EnableEndpointRouting = false);
            services.AddControllers();
            services.AddSwaggerGenOptions();
            services.AddHttpContextAccessor();
            services.AddOptions();
            services.Configure<BlockmainiaOptions>(Configuration);
            services.AddSyncProvider<CoinProto>("coins");
            services.AddPubSubProvider<CoinProto>(new Core.API.MQTT.NodeEndPoint(brokerSection.GetValue<string>("host"), brokerSection.GetValue<int>("port")));
            services.AddDbContext();
            services.AddUnitOfWork();
            services.AddOnionServiceClientConfiguration();
            services.AddOnionServiceClient();
            services.AddHttpClientService(gatewaySection.GetValue<string>("url"));
            services.AddHttpClientHandler<Startup>();
            services.AddBroadcastClient();
            services.AddTorClient<Startup>();
            services.AddMembershipServiceClient();
            services.AddActorSystem("coinapi");
            services.AddNetworkActorProvider<CoinProto>();
            services.AddSigningActorProvider();
            services.AddInterpretActorProvider<CoinProto>(InterpretBlockActor.Create);
            services.AddProcessActorProvider<CoinProto>();
            services.AddSipActorProvider<Startup, CoinProto>();
            services.AddBlockGraphService<CoinProto>();
            services.AddCoinService();
            services.AddVerifiableFunctionsActorProvider();

            // Fix Additional copy of services
            services.AddSingleton<IXmlRepository, DataProtectionKeyRepository>();
            services.AddDataProtection().AddKeyManagementOptions(options => options.XmlRepository = services.BuildServiceProvider().GetService<IXmlRepository>());
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

            app.UseSync<CoinProto>();
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
                app.ApplicationServices.GetService<IHttpClientService>().Dispose();
                app.ApplicationServices.GetService<ActorSystem>().Terminate().Wait();
            });
        }
    }
}
