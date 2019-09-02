using System;
using System.Diagnostics;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Core.API.Helper;
using Core.API.Model;
using MessagePool.API.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Swagger;

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

            CreateLMDBDirectory();
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddSwaggerGen(options =>
            {
                options.DescribeAllEnumsAsStrings();
                options.SwaggerDoc("v1", new Info
                {
                    Title = "Tangram Message Pool HTTP API",
                    Version = "v1",
                    Description = "Messages sent between sender and receiver.",
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

            services.AddSingleton<IUnitOfWork, UnitOfWork>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<UnitOfWork>>();
                var filePath = Configuration["LMDB:FilePath"];
                return new UnitOfWork(filePath, logger);
            });

            services.AddTransient<IMessagePoolService, MessagePoolService>();

            services.AddOptions();

            var container = new ContainerBuilder();
            container.Populate(services);

            return new AutofacServiceProvider(container.Build());
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
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
                   c.SwaggerEndpoint($"{ (!string.IsNullOrEmpty(pathBase) ? pathBase : string.Empty) }/swagger/v1/swagger.json", "MessagePool.API V1");
                   c.OAuthClientId("messagepoolswaggerui");
                   c.OAuthAppName("Message Pool Swagger UI");
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
