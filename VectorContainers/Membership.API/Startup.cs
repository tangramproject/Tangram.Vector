using Core.API.Onion;
using DotNetTor.SocksPort;
using Membership.API.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SwimProtocol;
using System;
using System.Diagnostics;
using System.Net;

namespace Membership.API
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

        IPAddress GetPublicHostIp(string serviceUrl = "https://ipv4.icanhazip.com/")
        {
            string ip = null;
            var membershipSection = Configuration.GetSection("membership");
            var publicAddressSection = membershipSection.GetSection("PublicHost");

            if (publicAddressSection.Exists())
                ip = publicAddressSection.Value;
            else
                ip = new WebClient()
                    .DownloadString(serviceUrl)
                    .Trim();

            return IPAddress.Parse(ip);
        }

        private string GetPublicHostPort()
        {
            string port = null;
            var membershipSection = Configuration.GetSection("membership");
            var publicPortSection = membershipSection.GetSection("PublicPort");

            if (publicPortSection.Exists())
                port = publicPortSection.Value;
            else
                port = "8080";

            return port;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddResponseCompression();

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
            services.AddMvc(option => option.EnableEndpointRouting = false);

            services.AddControllers()
                    .AddNewtonsoftJson();


            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    License = new Microsoft.OpenApi.Models.OpenApiLicense
                    {
                        Name = "Attribution-NonCommercial-NoDerivatives 4.0 International",
                        Url = new Uri("https://raw.githubusercontent.com/tangramproject/Tangram.Vector/initial/LICENSE")
                    },
                    Title = "Tangram Membership HTTP API",
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

            services.AddSingleton<IOnionServiceClientConfiguration, OnionServiceClientConfiguration>();

            services.AddHttpClient<IOnionServiceClient, OnionServiceClient>();

            services.AddSingleton(sp =>
            {
                var logger = sp.GetService<ILogger<Startup>>();

                //var onionStarted = sp.GetService<IOnionServiceClient>()
                //                     .IsTorStartedAsync()
                //                     .GetAwaiter()
                //                     .GetResult();

                //while(!onionStarted)
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

                var handler = new SocksPortHandler(onionServiceClientConfiguration.SocksHost, onionServiceClientConfiguration.SocksPort);

                return handler;
            });

            services.AddHttpClient<ITorClient, TorClient>()
                //.ConfigurePrimaryHttpMessageHandler(
                //    p => p.GetRequiredService<SocksPortHandler>()
                //)
                .SetHandlerLifetime(TimeSpan.FromMinutes(5));

            services.AddSingleton<ISwimNode, SwimNode>(sp =>
            {
                //var onionServiceClientDetails = sp.GetService<IOnionServiceClient>()
                //                                  .GetHiddenServiceDetailsAsync()
                //                                  .GetAwaiter()
                //                                  .GetResult();

                var publicIp = GetPublicHostIp();
                var publicPort = GetPublicHostPort();

                return new SwimNode($"http://{publicIp}:{publicPort}");
            });

            services.AddSingleton<ISwimProtocolProvider, SwimProtocolProvider>();
            services.AddSingleton<ISwimProtocol, FailureDetection>();

            services.AddHostedService<FailureDetection>();
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
                   c.SwaggerEndpoint($"{ (!string.IsNullOrEmpty(pathBase) ? pathBase : string.Empty) }/swagger/v1/swagger.json", "Membership.API V1");
                   c.OAuthClientId("membershipswaggerui");
                   c.OAuthAppName("Membership Swagger UI");
               });
        }
    }
}

