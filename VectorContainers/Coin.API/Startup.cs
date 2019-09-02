using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Core.API.Helper;
using Core.API.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Swagger;
using Coin.API.Services;
using System.Diagnostics;
using Core.API.Onion;
using System.Net;
using DotNetTor.SocksPort;
using Core.API.Membership;
using Core.API.Broadcast;

namespace Coin.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            AppDomain.CurrentDomain.FirstChanceException += (sender, eventArgs) =>
            {
                Debug.WriteLine(eventArgs.Exception.ToString());
            };

            CreateLMDBDirectory();
        }

        public IConfiguration Configuration { get; }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            services.AddSwaggerGen(options =>
            {
                options.DescribeAllEnumsAsStrings();
                options.SwaggerDoc("v1", new Info
                {
                    Title = "Tangram Coin HTTP API",
                    Version = "v1",
                    Description = "Backend services.",
                    TermsOfService = "https://tangrams.io/legal/",
                    Contact = new Contact() { Email = "dev@getsneak.org", Url = "https://tangrams.io/about-tangram/team/" }
                });
            });

            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
            });

            services.AddHttpContextAccessor();
            services.AddOptions();

            services.AddTransient<IBroadcastClient, BroadcastClient>();
            services.AddTransient<IMembershipServiceClient, MembershipServiceClient>();

            services.AddSingleton<IUnitOfWork, UnitOfWork>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<UnitOfWork>>();
                var filePath = Configuration["LMDB:FilePath"];
                return new UnitOfWork(filePath, logger);
            });

            services.AddSingleton<IBlockGraphService, BlockGraphService>(sp =>
            {
                var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
                var membershipServiceClient = sp.GetRequiredService<IMembershipServiceClient>();
                var onionServiceClient = sp.GetRequiredService<IOnionServiceClient>();
                var logger = sp.GetRequiredService<ILogger<BlockGraphService>>();
                var torClient = sp.GetRequiredService<ITorClient>();

                return new BlockGraphService(unitOfWork, membershipServiceClient, onionServiceClient, logger, torClient);         
            });

            services.AddTransient<ICoinService, CoinService>();

            services.AddSingleton<IOnionServiceClientConfiguration, OnionServiceClientConfiguration>();

            services.AddHttpClient<IOnionServiceClient, OnionServiceClient>();

            services.AddSingleton(sp =>
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                var onionServiceClientConfiguration = sp.GetService<IOnionServiceClientConfiguration>();

                var handler = new SocksPortHandler(onionServiceClientConfiguration.SocksHost, onionServiceClientConfiguration.SocksPort);

                return handler;
            });

            services.AddHttpClient<ITorClient, TorClient>()
                .ConfigurePrimaryHttpMessageHandler(
                    p => p.GetRequiredService<SocksPortHandler>()
                )
                .SetHandlerLifetime(TimeSpan.FromMinutes(5));

            var container = new ContainerBuilder();
            container.Populate(services);

            return new AutofacServiceProvider(container.Build());
        }

        public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            var pathBase = Configuration["PATH_BASE"];
            if (!string.IsNullOrEmpty(pathBase))
            {
                app.UsePathBase(pathBase);
            }

            app.UseStaticFiles();
            app.UseCors("CorsPolicy");
            app.UseMvcWithDefaultRoute();

            app.UseSwagger()
               .UseSwaggerUI(c =>
               {
                   c.SwaggerEndpoint($"{ (!string.IsNullOrEmpty(pathBase) ? pathBase : string.Empty) }/swagger/v1/swagger.json", "Coin.API V1");
                   c.OAuthClientId("coinswaggerui");
                   c.OAuthAppName("Coin Swagger UI");
               });
        }

        private void CreateLMDBDirectory()
        {
            var LmdbPath = System.IO.Path.Combine(Util.EntryAssemblyPath(), Configuration["LMDB:FilePath"]);
            if (!System.IO.Directory.Exists(LmdbPath))
                System.IO.Directory.CreateDirectory(LmdbPath);
        }
    }
}
