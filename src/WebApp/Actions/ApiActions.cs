using System;
using ExtCore.Infrastructure.Actions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Rewrite;

namespace WebApp.Actions
{
    public class ApiActions : IConfigureAction
    {
        public int Priority => 1;

        public void Execute(IApplicationBuilder app, IServiceProvider serviceProvider)
        {
            app.UseRewriter(new RewriteOptions()
                .Add(RewriteApiRequests));
        }
        public static void RewriteApiRequests(RewriteContext context)
        {
            var request = context.HttpContext.Request;
            var path = request.Path.Value;

            if(request.Host.Host.StartsWith("api."))
            {
                request.Path = "/api/" + request.Path.Value.TrimStart('/');
            }
        }
    }
}