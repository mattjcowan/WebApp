using System;
using ExtCore.Infrastructure.Actions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Rewrite;

namespace WebApp.Actions
{
    public class ApiActions : IConfigureAction
    {
        // make sure this runs BEFORE the std aspnet actions
        // otherwise there's a risk that static files get rendered
        public int Priority => AspNetStartupActions.PRIORITY_FLAG - 1;

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
                var pathValue = (request.Path.Value ?? "").Replace("api/", "");
                request.Path = "/api/" + request.Path.Value.TrimStart('/');
            }
        }
    }
}