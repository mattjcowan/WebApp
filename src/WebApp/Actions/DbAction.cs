using System;
using System.IO;
using ExtCore.Infrastructure.Actions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace WebApp.Actions
{
    public class DbAction : IConfigureServicesAction
    {
        private static string DataDir = null;
        public int Priority => int.MinValue;

        public void Execute(IServiceCollection services, IServiceProvider serviceProvider)
        {
            DataDir = Environment.GetEnvironmentVariable("DATA_DIR");
            if (string.IsNullOrWhiteSpace(DataDir))
            {
                DataDir = Path.Combine(serviceProvider.GetRequiredService<IHostingEnvironment>().ContentRootPath, "App_Data");
            }

            Directory.CreateDirectory(DataDir);

            var dbFactory = GetDbFactory(Environment.GetEnvironmentVariable("DB_DIALECT"), Environment.GetEnvironmentVariable("DB_CONNECTIONSTRING"));
            if (dbFactory != null)
            {
                services.AddSingleton<IDbConnectionFactory>(dbFactory);

                var appSettings = new OrmLiteAppSettings(dbFactory);
                appSettings.InitSchema();
                services.AddSingleton<IAppSettings>(appSettings);
            }
        }

        public static OrmLiteConnectionFactory GetDbFactory(string dialect, string connectionString)
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

            if (!string.IsNullOrWhiteSpace(DataDir))
            {
                connectionString = connectionString.Replace("~data/", DataDir.TrimEnd('/', '\\') + "/");
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
            catch(Exception ex)
            {
                Console.WriteLine("ERROR!! " + ex);
                return null;
            }
        }
    }
}