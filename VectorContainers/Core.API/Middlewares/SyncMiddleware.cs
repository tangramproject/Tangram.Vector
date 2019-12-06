using System;
using System.Linq;
using System.Threading.Tasks;
using Core.API.Model;
using Core.API.Providers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Core.API.Middlewares
{
    public class SyncMiddleware<TAttach>
    {
        private readonly RequestDelegate _next;

        public SyncMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext, SyncProvider<TAttach> syncProvider)
        {
            httpContext.Response.OnStarting(state =>
            {
                if (!syncProvider.IsSynchronized)
                {
                    var httpContext = (HttpContext)state;
                    var paths = new string[] { "blockgraph", "blockgraphs", "mempool", "Coin" };
                    var parts = httpContext.Request.Path.Value.Split('/');
                    var stop = paths.FirstOrDefault(parts.Contains);

                    if (!string.IsNullOrEmpty(stop))
                    {
                        httpContext.Response.Headers.Add("X-Response-Synchronized", new string[] { "false" });
                        httpContext.Response.StatusCode = StatusCodes.Status204NoContent;

                        if (!syncProvider.IsRunning)
                        {
                            _ = Task.Factory.StartNew(async () =>
                            {
                                await syncProvider.SynchronizeCheck();
                            });
                        }
                    }
                }

                return Task.CompletedTask;

            }, httpContext);

            await _next(httpContext);
        }
    }

    public static class SyncMiddlewareExtensions
    {
        public static IApplicationBuilder UseSync<TAttach>(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SyncMiddleware<TAttach>>();
        }
    }
}
