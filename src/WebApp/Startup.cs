using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ExtCore.Infrastructure.Actions;
using ExtCore.WebApplication;
using ExtCore.WebApplication.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace WebApp
{
    public class Startup
    {
        public const string ExcludeAssemblyScanningRegex = "^(runtime.*|Remotion.*|Oracle.*|Microsoft.*|Aws.*|Google.*|ExtCore.*|MySql.*|Newtonsoft.*|NETStandard.*|Npgsql.*|ServiceStack.*|SQLite.*|System.*|e_.*)$";

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddExtCore(null, false, new DefaultAssemblyProvider(services.BuildServiceProvider())
            {
                IsCandidateCompilationLibrary = (_ => !Regex.IsMatch(_.Name, ExcludeAssemblyScanningRegex, RegexOptions.IgnoreCase))
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseExtCore();
        }
    }

    public class DefaultServiceAction : IConfigureAction
    {
        public int Priority => int.MaxValue;

        public void Execute(IApplicationBuilder app, IServiceProvider serviceProvider)
        {
            var appName = app.ApplicationServices.GetRequiredService<IHostingEnvironment>().ApplicationName;
            app.Run(async (context) =>
            {
                await context.Response.WriteAsync($"{appName} (v{typeof(Startup).Assembly.GetName().Version.ToString(4)})");
            });
        }
    }
}
