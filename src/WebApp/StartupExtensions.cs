using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ExtCore.Infrastructure;
using ExtCore.WebApplication;
using ExtCore.WebApplication.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace WebApp
{
  public static class StartupExtensions
  {
    public const string DefaultExcludeAssemblyScanningRegex = "^(runtime.*|Remotion.*|Oracle.*|Microsoft.*|Aws.*|Google.*|ExtCore.*|MySql.*|Newtonsoft.*|NETStandard.*|Npgsql.*|ServiceStack.*|SQLite.*|System.*|e_.*)$";

    public static void AddWebApp(this IServiceCollection services, string excludeAssemblyScanningRegex = DefaultExcludeAssemblyScanningRegex)
    {
      services.AddExtCore(null, new DefaultAssemblyProvider(services.BuildServiceProvider())
      {
        IsCandidateCompilationLibrary = _ => IsCandidateCompilationLibraryFunc(_, excludeAssemblyScanningRegex)
      });
    }

    public static void AddWebApp(this IServiceCollection services, Func<Microsoft.Extensions.DependencyModel.Library, bool> isCandidateCompilationLibrary)
    {
      services.AddExtCore(null, new DefaultAssemblyProvider(services.BuildServiceProvider())
      {
        IsCandidateCompilationLibrary = _ => isCandidateCompilationLibrary != null ?
            isCandidateCompilationLibrary(_):
            IsCandidateCompilationLibraryFunc(_)
      });
    }

    public static bool IsCandidateCompilationLibraryFunc(Microsoft.Extensions.DependencyModel.Library lib, string excludeAssemblyScanningRegex = DefaultExcludeAssemblyScanningRegex)
    {
      return !Regex.IsMatch(lib.Name, excludeAssemblyScanningRegex, RegexOptions.IgnoreCase);
    }

    public static void UseWebApp(this IApplicationBuilder app)
    {
      var env = app.ApplicationServices.GetRequiredService<IHostingEnvironment>();

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
