using System;
using System.Diagnostics;
using Broker.API.Services;
using Broker.API.StartupExtentions;
using Core.API.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Broker.API
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
            var gatewaySection = Configuration.GetSection("Gateway");
            var brokerSection = Configuration.GetSection("Broker");

            services.AddResponseCompression();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
            services.AddMvc(option => option.EnableEndpointRouting = false);
            services.AddControllers();
            services.AddSwaggerGenOptions();
            services.AddHttpContextAccessor();
            services.AddOptions();
            services.AddOnionServiceClientConfiguration();
            services.AddOnionServiceClient();
            services.AddHttpClientService(gatewaySection.GetValue<string>("Url"));
            services.AddHttpClientHandler<Startup>();
            services.AddBroadcastClient();
            services.AddTorClient<Startup>();
            services.AddMembershipServiceClient();
            services.AddMqttService(gatewaySection.GetValue<int>("port"));
        }

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
                   c.SwaggerEndpoint($"{ (!string.IsNullOrEmpty(pathBase) ? pathBase : string.Empty) }/swagger/v1/swagger.json", "Broker.API V1");
                   c.OAuthClientId("brokerswaggerui");
                   c.OAuthAppName("Broker Swagger UI");
               });

            lifetime.ApplicationStarted.Register(() =>
            {
                app.ApplicationServices.GetService<MqttService>();
            });

            lifetime.ApplicationStopping.Register(() =>
            {
                app.ApplicationServices.GetService<MqttService>().Dispose();
            });
        }
    }
}
