// TGMNode by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using Akka.Actor;
using Microsoft.Extensions.Hosting;
using TGMNode.StartupExtensions;
using TGMNode.Model;
using TGMNode.Actors;
using TGMCore.Providers;
using TGMCore.Consensus;
using TGMCore.Extensions;

namespace TGMNode
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

            services.AddResponseCompression();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
            services.AddMvc(option => option.EnableEndpointRouting = false);
            services.AddControllers();
            services.AddSwaggerGenOptions();
            services.AddHttpContextAccessor();
            services.AddOptions();
            services.Configure<BlockmainiaOptions>(Configuration);
            services.AddDbContext();
            services.AddUnitOfWork();
            services.AddDataKeysProtection();
            services.AddActorSystem("tangram-system", "tgmnode.hocon");
            services.AddSigningActorProvider();
            services.AddInterpretActorProvider<TransactionProto>(InterpretBlockActor.Create);
            services.AddProcessActorProvider<TransactionProto>();
            services.AddSipActorProvider<Startup, TransactionProto>();
            services.AddBlockGraphService<TransactionProto>();
            services.AddTransactionService();
            services.AddVerifiableFunctionsActorProvider();
            services.AddClusterProvider("tgmnode.hocon");
            services.AddPublisherProvider<TransactionProto>("blockgraph");
            services.AddSubscriberProvider<TransactionProto>("blockgraph");
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
                   c.SwaggerEndpoint($"{ (!string.IsNullOrEmpty(pathBase) ? pathBase : string.Empty) }/swagger/v1/swagger.json", "TGMNode V1");
                   c.OAuthClientId("tgmnodeswaggerui");
                   c.OAuthAppName("TGMNode Swagger UI");
               });

            lifetime.ApplicationStarted.Register(() =>
            {
                app.ApplicationServices.GetService<ActorSystem>();
                app.ApplicationServices.GetService<ISipActorProvider>();
                app.ApplicationServices.GetService<ISubscriberBaseGraphProvider>();
            });

            lifetime.ApplicationStopping.Register(() =>
            {
                app.ApplicationServices.GetService<ActorSystem>().Terminate().Wait();
            });
        }
    }
}
