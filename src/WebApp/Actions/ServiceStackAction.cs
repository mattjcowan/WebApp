using System;
using System.Collections.Generic;
using System.Linq;
using ExtCore.Infrastructure;
using ExtCore.Infrastructure.Actions;
using Funq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack;
using ServiceStack.Configuration;
using ServiceStack.Data;
using WebApp.Plugins;

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
    public class AppHost : AppHostBase
    {
        public AppHost(IHostingEnvironment hostingEnvironment,
                       IDbConnectionFactory dbConnectionFactory,
                       IAppSettings appSettings,
                       IEnumerable<IPlugin> dynamicPlugins) : base(
                           appSettings.GetNullableString("ServiceName") ?? hostingEnvironment.ApplicationName,
                           typeof(AppHost).Assembly)
        {
            HostingEnvironment = hostingEnvironment;
            HostingEnvironment.ApplicationName = ServiceName;
            DbFactory = dbConnectionFactory;
            AppSettings = appSettings;
            DynamicPlugins = dynamicPlugins?.ToList() ?? new List<IPlugin>();
        }

        public IHostingEnvironment HostingEnvironment { get; }
        public IDbConnectionFactory DbFactory { get; }
        public List<IPlugin> DynamicPlugins { get; }

        public override void Configure(Container container)
        {
            if (container.TryResolve<IDbConnectionFactory>() == null)
              container.Register<IDbConnectionFactory>(this.DbFactory);

            DynamicPlugins.Where(p => p is ICanPreRegister).Cast<ICanPreRegister>().OrderBy(p => p.PreRegistrationPriority).Each(plugin => {
                (plugin as ICanPreRegister).PreRegister(this, container);
            });

            DynamicPlugins.Each(plugin => {
                Plugins.Add(plugin);
            });
        }
    }
}
