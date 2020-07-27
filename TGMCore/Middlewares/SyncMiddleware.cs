// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Linq;
using System.Threading.Tasks;
using TGMCore.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace TGMCore.Middlewares
{
    //public class SyncMiddleware<TAttach>
    //{
    //    private readonly RequestDelegate _next;

    //    public SyncMiddleware(RequestDelegate next)
    //    {
    //        _next = next;
    //    }

    //    public async Task Invoke(HttpContext httpContext, SyncProvider<TAttach> syncProvider)
    //    {
    //        httpContext.Response.OnStarting(state =>
    //        {
    //            if (!syncProvider.IsSynchronized)
    //            {
    //                var httpContext = (HttpContext)state;
    //                var paths = new string[] { "blockgraph", "blockgraphs", "mempool", "Coin" };
    //                var parts = httpContext.Request.Path.Value.Split('/');
    //                var stop = paths.FirstOrDefault(parts.Contains);

    //                if (!string.IsNullOrEmpty(stop))
    //                {
    //                    httpContext.Response.Headers.Add("X-Response-Synchronized", new string[] { "false" });
    //                    httpContext.Response.StatusCode = StatusCodes.Status204NoContent;

    //                    if (!syncProvider.IsRunning)
    //                    {
    //                        _ = Task.Factory.StartNew(async () =>
    //                        {
    //                            await syncProvider.SynchronizeCheck();
    //                        });
    //                    }
    //                }
    //            }

    //            return Task.CompletedTask;

    //        }, httpContext);

    //        await _next(httpContext);
    //    }
    //}

    //public static class SyncMiddlewareExtensions
    //{
    //    public static IApplicationBuilder UseSync<TAttach>(this IApplicationBuilder builder)
    //    {
    //        return builder.UseMiddleware<SyncMiddleware<TAttach>>();
    //    }
    //}
}
