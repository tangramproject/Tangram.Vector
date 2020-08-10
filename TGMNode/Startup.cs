// TGMNode by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Akka.Actor;
using Microsoft.Extensions.Hosting;
using TGMNode.StartupExtensions;
using TGMNode.Model;
using TGMNode.Actors;
using TGMCore.Providers;
using TGMCore.Consensus;
using TGMCore.Extensions;
using Autofac.Extensions.DependencyInjection;
using Autofac;
using Microsoft.AspNetCore.Hosting;

namespace TGMNode
{
    public class Startup
    {
        public Startup(IWebHostEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; private set; }

        public ILifetimeScope AutofacContainer { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            var dataProtecttion = Configuration.GetSection("DataProtectionPath");

            services.AddResponseCompression();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
            services.AddMvc(option => option.EnableEndpointRouting = false);
            services.AddControllers();
            services.AddSwaggerGenOptions();
            services.AddHttpContextAccessor();
            services.AddOptions();
            services.Configure<BlockmainiaOptions>(Configuration);
            services.AddDataKeysProtection(dataProtecttion.Value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.AddDbContext();
            builder.AddUnitOfWork();
            builder.AddActorSystem("tangram-system", "tgmnode.hocon");
            builder.AddSigningActorProvider();
            builder.AddInterpretActorProvider<TransactionProto>(InterpretBlockActor.Create);
            builder.AddProcessActorProvider<TransactionProto>();
            builder.AddSipActorProvider<Startup, TransactionProto>();
            builder.AddBlockGraphService<TransactionProto>();
            builder.AddTransactionService();
            builder.AddVerifiableFunctionsActorProvider();
            builder.AddClusterProvider("tgmnode.hocon");
            builder.AddPublisherProvider<TransactionProto>("blockgraph");
            builder.AddSubscriberProvider<TransactionProto>("blockgraph");
            builder.AddDataKeysProtection();
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

            AutofacContainer = app.ApplicationServices.GetAutofacRoot();
            AutofacContainer.Resolve<ActorSystem>().UseAutofac(AutofacContainer);

            lifetime.ApplicationStarted.Register(() =>
            {
                AutofacContainer.Resolve<ActorSystem>();
                AutofacContainer.Resolve<ISipActorProvider>();
                AutofacContainer.Resolve<ISubProvider>();
            });

            lifetime.ApplicationStopping.Register(() =>
            {
                AutofacContainer.Resolve<ActorSystem>().Terminate().Wait();
            });


        }
    }
}
