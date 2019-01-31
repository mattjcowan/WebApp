using System;
using ExtCore.Infrastructure.Actions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Net.Http.Headers;

namespace WebApp.Actions
{
    public class ServiceWorkerAction : IConfigureAction
    {
        public int Priority => Priorities.ServiceWorkerAction;

        public void Execute(IApplicationBuilder app, IServiceProvider serviceProvider)
        {
          app.Use(async (httpContext, next) =>
          {
              if ((httpContext.Request.Path.Value ?? "").Contains("/sw.js"))
              {
                httpContext.Response.Headers[HeaderNames.Expires] = "off";
                httpContext.Response.Headers[HeaderNames.CacheControl] = "no-store";
              }
              await next();
          });
        }
    }
}
