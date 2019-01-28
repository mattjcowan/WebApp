using System;
using System.Linq;
using ExtCore.Infrastructure;
using ExtCore.Infrastructure.Actions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack;
using ServiceStack.Configuration;
using ServiceStack.Data;

namespace WebApp.Actions
{

    public class ServiceStackAction : IConfigureAction, IConfigureServicesAction
    {
        public int Priority => 1;

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

        }
    }
}