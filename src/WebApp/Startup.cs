using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ExtCore.WebApplication;
using ExtCore.WebApplication.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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

            if (string.IsNullOrWhiteSpace(env.WebRootPath))
            {
                env.WebRootPath = Path.Combine(env.ContentRootPath, "wwwroot");
            }

            if (!Directory.Exists(env.WebRootPath))
            {
                Directory.CreateDirectory(env.WebRootPath);
            }

            app.UseExtCore();
        }
    }
}
