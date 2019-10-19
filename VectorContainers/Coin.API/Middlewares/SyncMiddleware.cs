using System.Linq;
using System.Threading.Tasks;
using Coin.API.Providers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Coin.API.Middlewares
{
    public class SyncMiddleware
    {
        private readonly RequestDelegate _next;

        public SyncMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext, SyncProvider syncProvider)
        {
            httpContext.Response.OnStarting(async state =>
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
                        await httpContext.Response.WriteAsync("Node out of sync");

                        if (!syncProvider.IsRunning)
                        {
                            _ = Task.Factory.StartNew(async () =>
                            {
                                await syncProvider.SynchronizeCheck();
                            });
                        }
                    }
                }
            }, httpContext);

            await _next(httpContext);
        }
    }

    public static class SyncMiddlewareExtensions
    {
        public static IApplicationBuilder UseSync(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SyncMiddleware>();
        }
    }
}
