using System;
using System.IO;
using ExtCore.Infrastructure.Actions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace WebApp.Actions
{
    public abstract class ActionBase : IConfigureServicesAction, IConfigureAction
    {
        public virtual int Priority => Priorities.DefaultAction;

        public void Execute(IServiceCollection serviceCollection, IServiceProvider serviceProvider)
        {
            var dataDir = Environment.GetEnvironmentVariable("DATA_DIR");
            if (string.IsNullOrWhiteSpace(dataDir))
            {
                dataDir = Path.Combine(serviceProvider.GetRequiredService<IHostingEnvironment>().ContentRootPath, "App_Data");
            }

            dataDir = Path.GetFullPath(dataDir);
            if (!Directory.Exists(dataDir))
              Directory.CreateDirectory(dataDir);

            // Console.WriteLine("Data root path: " + dataDir);
            Execute(serviceCollection, serviceProvider, dataDir);
        }

        public virtual void Execute(IServiceCollection serviceCollection, IServiceProvider serviceProvider, string dataDir)
        {
        }

        public virtual void Execute(IApplicationBuilder applicationBuilder, IServiceProvider serviceProvider)
        {
        }
    }
}
