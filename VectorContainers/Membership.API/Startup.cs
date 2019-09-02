using Autofac;
using Autofac.Extensions.DependencyInjection;
using Core.API.Onion;
using DotNetTor.SocksPort;
using Membership.API.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Swagger;
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

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddSwaggerGen(options =>
            {
                options.DescribeAllEnumsAsStrings();
                options.SwaggerDoc("v1", new Info
                {
                    Title = "Tangram Membership HTTP API",
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

            services.AddSingleton<ISwimNode, SwimNode>(sp =>
            {
                var onionServiceClientDetails = sp.GetService<IOnionServiceClient>()
                                                  .GetHiddenServiceDetailsAsync()
                                                  .GetAwaiter()
                                                  .GetResult();

                return new SwimNode($"http://{onionServiceClientDetails.Hostname}");
            });

            services.AddSingleton<ISwimProtocolProvider, SwimProtocolProvider>();
            services.AddSingleton<IHostedService, FailureDetection>();

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

            //app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCors("CorsPolicy");
            app.UseMvcWithDefaultRoute();

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

