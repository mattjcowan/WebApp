using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ExtCore.Infrastructure;
using ExtCore.Infrastructure.Actions;
using ExtCore.WebApplication;
using ExtCore.WebApplication.Extensions;
using Funq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using ServiceStack;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace WebApp
{
    public class Startup
    {
        public const string ExcludeAssemblyScanningRegex = "^(runtime.*|Remotion.*|Oracle.*|Microsoft.*|Aws.*|Google.*|ExtCore.*|MySql.*|Newtonsoft.*|NETStandard.*|Npgsql.*|ServiceStack.*|SQLite.*|System.*|e_.*)$";
        public static readonly DateTime StartedAt = DateTime.UtcNow;

        public Startup(IHostingEnvironment hostingEnvironment, IConfiguration configuration)
        {
            this.hostingEnvironment = hostingEnvironment;
            this.configuration = configuration;
        }

        private readonly IHostingEnvironment hostingEnvironment;
        private readonly IConfiguration configuration;
        private string _dataDir = null;
        public string DATA_DIR
        {
            get
            {
                if (_dataDir == null)
                {
                    var dir = Environment.GetEnvironmentVariable("DATA_DIR") ??
                        configuration.GetValue<string>("App:DataDir", null);
                    if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                        _dataDir = Path.GetFullPath(dir);
                }
                return _dataDir;
            }
        }

        private string _pluginsDir = null;
        public string PLUGINS_DIR
        {
            get
            {
                if (_pluginsDir == null)
                {
                    var dir = Environment.GetEnvironmentVariable("PLUGINS_DIR") ??
                        configuration.GetValue<string>("App:PluginsDir", null);
                    if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                        _pluginsDir = Path.GetFullPath(dir);
                }
                return _pluginsDir;
            }
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // require some defaults
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });

            services.AddCors(options => options.AddPolicy("CorsPolicy", builder => { builder.AllowAnyMethod().AllowAnyHeader().AllowAnyOrigin(); }));

            // register database connections if applicable
            RegisterDbConnectionFactories(services);

            services.AddExtCore(PLUGINS_DIR, new DefaultAssemblyProvider(services.BuildServiceProvider())
            {
                IsCandidateCompilationLibrary = _ => !Regex.IsMatch(_.Name, ExcludeAssemblyScanningRegex, RegexOptions.IgnoreCase),
                    IsCandidateAssembly = _ => !Regex.IsMatch(_.GetName().Name, ExcludeAssemblyScanningRegex, RegexOptions.IgnoreCase)
            });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            RegisterServiceStackLicense(env);

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

            // do not cache any service worker files (use typical names to recognize them)
            app.Use(async(httpContext, next) =>
            {
                if ((httpContext.Request.Path.Value ?? "").Contains("/sw.js") ||
                    (httpContext.Request.Path.Value ?? "").Contains("/service-worker.js") ||
                    (httpContext.Request.Path.Value ?? "").Contains("/serviceworker.js"))
                {
                    httpContext.Response.Headers[HeaderNames.Expires] = "off";
                    httpContext.Response.Headers[HeaderNames.CacheControl] = "no-store";
                }
                await next();
            });

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseExtCore();

            app.Run(async(context) =>
            {
                context.Response.Headers[HttpHeaders.ContentType] = MimeTypes.Json;
                await context.Response.WriteAsync(new
                {
                    env.ApplicationName,
                        Version = typeof(Startup).Assembly.GetName().Version.ToString(4),
                        Host = context.Request.Host.Value,
                        StartedAt
                }.ToJson());
            });
        }

        private void RegisterServiceStackLicense(IHostingEnvironment env)
        {
            var licenseFileName = "servicestack.license";
            var licenseFile = Path.Combine(env.ContentRootPath, licenseFileName);

            if (File.Exists(licenseFile))
            {
                Licensing.RegisterLicenseFromFile(licenseFile);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(this.DATA_DIR))
                {
                    Licensing.RegisterLicenseFromFileIfExists(Path.Combine(this.DATA_DIR, licenseFileName));
                }
            }
        }

        private void RegisterDbConnectionFactories(IServiceCollection services)
        {
            var dbConfig = configuration.GetSection("Db").Get<DbConfig>();

            if (dbConfig == null)
            {
                if (string.IsNullOrWhiteSpace(this.DATA_DIR))
                    return;

                // if there's a db.config.json file in the data directory,
                // use that instead
                var configFile = Path.Combine(this.DATA_DIR, "db.json");
                if (!File.Exists(configFile))
                    configFile = Path.Combine(this.DATA_DIR, "config.db.json");
                if (!File.Exists(configFile))
                    return;

                try
                {
                    dbConfig = File.ReadAllText(configFile).FromJson<DbConfig>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR!! " + ex);
                    return;
                }
            }

            if (dbConfig == null)
                return;

            var dbFactory = GetDbFactory(dbConfig.Dialect, dbConfig.ConnectionString);
            if (dbConfig.NamedConnections != null)
            {
                foreach (var nc in dbConfig.NamedConnections)
                {
                    var ncDbFactory = GetDbFactory(nc.Value.Dialect, nc.Value.ConnectionString);
                    if (ncDbFactory != null)
                    {
                        OrmLiteConnectionFactory.NamedConnections[nc.Key] = ncDbFactory;
                    }
                }
            }

            if (dbFactory != null)
            {
                services.AddSingleton<IDbConnectionFactory>(dbFactory);

                var appSettings = new OrmLiteAppSettings(dbFactory);
                appSettings.InitSchema();
                services.AddSingleton<IAppSettings>(appSettings);
            }
            else
            {
                Console.WriteLine("WARN!! Initialize a database connection with a `db.json` file in the data directory");
            }
        }

        public static OrmLiteConnectionFactory GetDbFactory(string dialect, string connectionString, string dataDir = null)
        {
            if (string.IsNullOrWhiteSpace(dialect) ||
                string.IsNullOrWhiteSpace(connectionString))
            {
                return null;
            }

            IOrmLiteDialectProvider dialectProvider = null;

            var dialectLowerCase = dialect.ToLowerInvariant();
            if (dialectLowerCase.Contains("sqlite"))
            {
                dialectProvider = SqliteDialect.Provider;
            }
            else if (dialectLowerCase.Contains("pgsql") || dialectLowerCase.Contains("postgres"))
            {
                dialectProvider = PostgreSqlDialect.Provider;
            }
            else if (dialectLowerCase.Contains("mysql"))
            {
                dialectProvider = MySqlDialect.Provider;
            }
            else if (dialectLowerCase.Contains("sqlserver"))
            {
                if (dialectLowerCase.Contains("2017"))
                {
                    dialectProvider = SqlServer2017Dialect.Provider;
                }
                else if (dialectLowerCase.Contains("2016"))
                {
                    dialectProvider = SqlServer2016Dialect.Provider;
                }
                else if (dialectLowerCase.Contains("2014"))
                {
                    dialectProvider = SqlServer2014Dialect.Provider;
                }
                else if (dialectLowerCase.Contains("2012"))
                {
                    dialectProvider = SqlServer2012Dialect.Provider;
                }
                else if (dialectLowerCase.Contains("2008"))
                {
                    dialectProvider = SqlServer2012Dialect.Provider;
                }
                else
                {
                    dialectProvider = SqlServerDialect.Provider;
                }
            }

            if (dialectProvider == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(dataDir))
            {
                connectionString = connectionString.Replace("~data/", dataDir.TrimEnd('/', '\\') + "/");
            }

            try
            {
                var dbFactory = new OrmLiteConnectionFactory(connectionString, dialectProvider);
                using(var db = dbFactory.OpenDbConnection())
                {
                    db.TableExists("this_table_does_not_exist");
                }
                return dbFactory;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR!! " + ex);
                return null;
            }
        }
    }

    public class DbConfig : DbConnectionDefinition
    {
        public Dictionary<string, DbConnectionDefinition> NamedConnections { get; set; }
    }

    public class DbConnectionDefinition
    {
        public string Dialect { get; set; }
        public string ConnectionString { get; set; }
    }

    public class ServiceStackAction : IConfigureAction, IConfigureServicesAction
    {
        public int Priority => 1000;

        public void Execute(IServiceCollection services, IServiceProvider serviceProvider)
        {
            Console.WriteLine("Setting JsConfig");
            JsConfig.DateHandler = DateHandler.ISO8601;
            JsConfig.TimeSpanHandler = TimeSpanHandler.DurationFormat;
            JsConfig.ExcludeTypeInfo = true;
            JsConfig.IncludeNullValues = true;
            JsConfig.IncludeNullValuesInDictionaries = true;
            JsConfig.TreatEnumAsInteger = false;

            JsConfig<Version>.SerializeFn = _ => _.ToString(4);
            JsConfig<Version>.DeSerializeFn = _ => new Version(_);

            if (serviceProvider.GetService<IDbConnectionFactory>() != null &&
                serviceProvider.GetService<IAppSettings>() != null)
            {
                foreach (var pluginType in ExtensionManager.GetImplementations<IPlugin>().Where(t => !t.IsAbstract && !t.IsInterface && t.IsClass))
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
            appSettings.GetNullableString("ApplicationName") ?? hostingEnvironment.ApplicationName,
            typeof(AppHost).Assembly)
        {
            HostingEnvironment = hostingEnvironment;
            HostingEnvironment.ApplicationName = ServiceName;
            DbFactory = dbConnectionFactory;
            AppSettings = appSettings;
            DynamicPlugins = dynamicPlugins?.ToList() ?? new List<IPlugin>();

            DynamicPlugins.Each(p =>
            {
                var initMethod = p.GetType().GetMethod("Init", BindingFlags.Public);
                if (initMethod != null)
                {
                    try
                    {
                        initMethod.Invoke(p, new [] { this });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"WARN!! Unable to initialize plugin {p.GetType().AssemblyQualifiedName} in appHost constructor: " + ex.Message);
                    }
                }
            });
        }

        public IHostingEnvironment HostingEnvironment { get; }
        public IDbConnectionFactory DbFactory { get; }
        public List<IPlugin> DynamicPlugins { get; }

        public override void Configure(Container container)
        {
            DynamicPlugins.Each(p =>
            {
                var preRegisterMethod = p.GetType().GetMethod("PreRegister", BindingFlags.Public);
                if (preRegisterMethod != null)
                {
                    try
                    {
                        preRegisterMethod.Invoke(p, new [] { this });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"WARN!! Unable to pre-register plugin {p.GetType().AssemblyQualifiedName}: " + ex.Message);
                    }
                }
            });

            DynamicPlugins.Each(plugin =>
            {
                Plugins.Add(plugin);
            });
        }
    }
}