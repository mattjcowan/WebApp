using System;
using ExtCore.Infrastructure.Actions;
using Microsoft.AspNetCore.Builder;

namespace WebApp.Actions
{
    public class StaticFilesAction : IConfigureAction
    {
        public int Priority => Priorities.StaticFilesAction;

        public void Execute(IApplicationBuilder app, IServiceProvider serviceProvider)
        {        
            app.UseDefaultFiles();
            app.UseStaticFiles();
        }
    }
}

