using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Coin.API.Services;
using System.Diagnostics;
using Core.API.Onion;
using System.Net;
using Core.API.Membership;
using Core.API.Broadcast;
using MihaZupan;
using System.Net.Http;
using Coin.API.Middlewares;
using Core.API.Model;
using Coin.API.Providers;
using System.Threading;

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

            //services.AddCors(options =>
            //{
            //    options.AddPolicy("default",
            //        builder => builder.AllowAnyOrigin()
            //        .AllowAnyMethod()
            //        .AllowAnyHeader()
            //        .AllowCredentials());
            //});

            services.AddHttpContextAccessor();

            services.AddOptions();

            services.AddTransient<InterpretBlocksProvider>();
            services.AddTransient<NetworkProvider>();
            services.AddTransient<SigningProvider>();

            services.AddSingleton<SyncProvider>();
            services.AddHostedService<SyncService>();

            services.AddSingleton<HierarchicalDataProvider>();
            services.AddHostedService<HierarchicalDataService>();

            services.AddSingleton<ReplyProvider>();
            services.AddHostedService<ReplyDataService>();

            services.AddSingleton<MissingBlocksProvider>();
            services.AddHostedService<MissingBlocksService>();

            services.AddSingleton<IDbContext, DbContext>();
            services.AddSingleton<IUnitOfWork, UnitOfWork>();

            services.AddSingleton<IOnionServiceClientConfiguration, OnionServiceClientConfiguration>();

            services.AddHttpClient<IOnionServiceClient, OnionServiceClient>();

            services.AddSingleton<IHttpService, HttpService>();

            services.AddSingleton(sp =>
            {
                var logger = sp.GetService<ILogger<Startup>>();

                //var onionStarted = sp.GetService<IOnionServiceClient>()
                //                     .IsTorStartedAsync()
                //                     .GetAwaiter()
                //                     .GetResult();

                //while (!onionStarted)
                //{
                //    logger.LogWarning("Unable to verify Tor is started... retrying in a few seconds");
                //    Thread.Sleep(5000);
                //    onionStarted = sp.GetService<IOnionServiceClient>()
                //                     .IsTorStartedAsync()
                //                     .GetAwaiter()
                //                     .GetResult();
                //}

                //logger.LogInformation("Tor is started... configuring Socks Port Handler");

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                var onionServiceClientConfiguration = sp.GetService<IOnionServiceClientConfiguration>();

                var proxy = new HttpToSocks5Proxy(new[]
                {
                    new ProxyInfo(onionServiceClientConfiguration.SocksHost, onionServiceClientConfiguration.SocksPort)
                });

                var handler = new HttpClientHandler { Proxy = proxy };

                return handler;
            });

            services.AddTransient<IBroadcastClient, BroadcastClient>();

            services.AddHttpClient<ITorClient, TorClient>()
                    //.ConfigurePrimaryHttpMessageHandler(p => p.GetRequiredService<HttpClientHandler>())
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

            services.AddTransient<IMembershipServiceClient, MembershipServiceClient>();

            services.AddSingleton<IBlockGraphService, BlockGraphService>(sp =>
            {
                var logger = sp.GetService<ILogger<Startup>>();
                var syncProvider = sp.GetService<SyncProvider>();

                while (syncProvider.IsRunning)
                {
                    logger.LogInformation("Syncing node... retrying in a few seconds");
                    Thread.Sleep(5000);
                }

                var blockGraphService = new BlockGraphService(
                    sp.GetService<IUnitOfWork>(),
                    sp.GetService<IHttpService>(),
                    sp.GetService<HierarchicalDataProvider>(),
                    sp.GetService<SigningProvider>(),
                    sp.GetService<InterpretBlocksProvider>(),
                    sp.GetService<ITorClient>(),
                    sp.GetService<ILogger<BlockGraphService>>());

                return blockGraphService;

            });

            services.AddTransient<ICoinService, CoinService>();
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
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
        }
    }
}
