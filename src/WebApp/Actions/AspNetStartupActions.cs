using System;
using System.IO;
using ExtCore.Infrastructure.Actions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace WebApp.Actions
{
    public class AspNetStartupActions : IConfigureAction, IConfigureServicesAction
    {
        public int Priority => Priorities.AspNetStartupActions;

        public void Execute(IServiceCollection services, IServiceProvider serviceProvider)
        {        
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });

            services.AddCors(options => options.AddPolicy("CorsPolicy", builder => { builder.AllowAnyMethod().AllowAnyHeader().AllowAnyOrigin(); }));
        }

        public void Execute(IApplicationBuilder app, IServiceProvider serviceProvider)
        {        
            var env = serviceProvider.GetService<IHostingEnvironment>();

            app.UseForwardedHeaders();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseCors("CorsPolicy");

            app.UseDefaultFiles();
            app.UseStaticFiles();
            
            var appContentDir = Path.Combine(env.ContentRootPath, "wwwroot", "app");
            Directory.CreateDirectory(appContentDir);
            if (!File.Exists(Path.Combine(appContentDir, "index.html"))) {
                File.WriteAllText(Path.Combine(appContentDir, "index.html"), @"
<html><body>Welcome to app</body></html>                
                ");
            }

            app.MapWhen(ctx => ctx.Request.Host.Value.StartsWith("app."), builder => {                
                builder.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(appContentDir)
                });
            });
        }
    }
}
