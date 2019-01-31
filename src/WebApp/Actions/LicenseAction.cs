using System;
using System.IO;
using ExtCore.Infrastructure.Actions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack;

namespace WebApp.Actions
{
    public class LicenseAction : ActionBase
    {
        public override int Priority => Priorities.LicenseAction;

        public override void Execute(IServiceCollection serviceCollection, IServiceProvider serviceProvider, string dataDir)
        {
          var licenseFileName = "servicestack.license";
          var rootDir = serviceProvider.GetRequiredService<IHostingEnvironment>().ContentRootPath;
          if (File.Exists(Path.Combine(rootDir, licenseFileName)))
            Licensing.RegisterLicenseFromFile(Path.Combine(rootDir, licenseFileName));
          else if (File.Exists(Path.Combine(dataDir, licenseFileName)))
            Licensing.RegisterLicenseFromFile(Path.Combine(dataDir, licenseFileName));
        }
    }
}
