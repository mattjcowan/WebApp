﻿using System;
using System.IO;
using ExtCore.Infrastructure.Actions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace WebApp.Actions
{
    public class HeadersAction : IConfigureAction, IConfigureServicesAction
    {
        public int Priority => Priorities.HeadersAction;

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
        }
    }
}

