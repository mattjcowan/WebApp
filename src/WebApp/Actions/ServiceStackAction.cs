using System;
using System.Linq;
using ExtCore.Infrastructure;
using ExtCore.Infrastructure.Actions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack;
using ServiceStack.Configuration;
using ServiceStack.Data;

namespace WebApp.Actions
{

    public class ServiceStackAction : IConfigureAction, IConfigureServicesAction
    {
        public int Priority => Priorities.ServiceStackAction;

        public void Execute(IServiceCollection services, IServiceProvider serviceProvider)
        {
            if (serviceProvider.GetService<IDbConnectionFactory>() != null &&
                serviceProvider.GetService<IAppSettings>() != null)
                {
                    foreach(var pluginType in ExtensionManager.GetImplementations<IPlugin>().Where(t => !t.IsAbstract && !t.IsInterface && t.IsClass))
                    {
                        services.AddSingleton(typeof(IPlugin), pluginType);
                    }

                    services.AddSingleton<IAppHost, AppHost>();
                }
        }

        public void Execute(IApplicationBuilder app, IServiceProvider serviceProvider)
        {
            var appHost = app.ApplicationServices.GetService<IAppHost>() as AppHostBase;
            if (appHost != null)
            {
                app.UseServiceStack(appHost);
            }
        }
    }

    public class FallbackService: Service
    {
        public object Any(Fallback request)
        {
            var host = ServiceStackHost.Instance;
            return new 
            {
                ServiceName = host.ServiceName,
                ApiVersion = host.Config.ApiVersion,
                StartedAt = host.StartedAt,
                PathInfo = "/" + (request.PathInfo ?? string.Empty),
                Host = Request.GetUrlHostName(),
                RemoteIp = Request.RemoteIp
            };
        }
    }

    [FallbackRoute("/{PathInfo*}")]
    public class Fallback
    {
        public string PathInfo { get; set; }
    }
}