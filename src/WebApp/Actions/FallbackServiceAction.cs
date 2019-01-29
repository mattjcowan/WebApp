using System;
using ExtCore.Infrastructure.Actions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack;

namespace WebApp.Actions
{
    public class FallbackServiceAction : IConfigureAction
    {
        public int Priority => Priorities.FallbackServiceAction;

        public void Execute(IApplicationBuilder app, IServiceProvider serviceProvider)
        {
            var appName = app.ApplicationServices.GetRequiredService<IHostingEnvironment>().ApplicationName;
            app.Run(async (context) =>
            {
                await context.Response.WriteAsync($"{appName} (v{typeof(Startup).Assembly.GetName().Version.ToString(4)} at {context.Request.Host.Value})");
            });
        }
    }
}
