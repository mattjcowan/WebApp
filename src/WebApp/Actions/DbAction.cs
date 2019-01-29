using System;
using System.Collections.Generic;
using System.IO;
using ExtCore.Infrastructure.Actions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack;
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

            var dbConfig = GetDbConfig() ?? new DbConfig();
            if (string.IsNullOrWhiteSpace(dbConfig.Dialect) || 
                string.IsNullOrWhiteSpace(dbConfig.ConnectionString))
            {
                dbConfig.Dialect = Environment.GetEnvironmentVariable("DB_DIALECT");
                dbConfig.ConnectionString = Environment.GetEnvironmentVariable("DB_CONNECTIONSTRING");
            }
            
            var dbFactory = GetDbFactory(dbConfig.Dialect, dbConfig.ConnectionString);
            if (dbConfig.NamedConnections != null)
            {
                foreach(var nc in dbConfig.NamedConnections)
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
        }

        internal static DbConfig GetDbConfig()
        {
            // if there's a db.config.json file in the data directory,
            // use that instead
            var configFile = Path.Combine(DataDir, "db.config.json");
            if (File.Exists(configFile))
            {
                return File.ReadAllText(configFile).FromJson<DbConfig>();
            }
            
            return null;
        }

        internal static void ClearDbConfig(string namedConnection = null)
        {
            var dbConfig = GetDbConfig();
            if (dbConfig == null) return; // good to go

            if (string.IsNullOrWhiteSpace(namedConnection))
            {
                dbConfig.Dialect = null;
                dbConfig.ConnectionString = null;
            }
            else
            {
                if (dbConfig.NamedConnections == null ||
                    dbConfig.NamedConnections.Keys.Count == 0 ||
                    !dbConfig.NamedConnections.ContainsKey(namedConnection)) 
                    return; // good to go
                
                dbConfig.NamedConnections.RemoveKey(namedConnection);
            }

            var configFile = Path.Combine(DataDir, "db.config.json");

            // attempt to delete config file if no connections exist
            if (string.IsNullOrWhiteSpace(dbConfig.Dialect) &&
                string.IsNullOrWhiteSpace(dbConfig.ConnectionString) &&
                (dbConfig.NamedConnections == null ||
                 dbConfig.NamedConnections.Keys.Count == 0))
            {
                if (File.Exists(configFile))
                {
                    try
                    {
                        File.Delete(configFile);
                        return;
                    } catch {}
                }
            }

            Directory.CreateDirectory(DataDir);
            File.WriteAllText(configFile, dbConfig.ToJson());
        }

        internal static void SetDbConfig(string dialect, string connectionString, string namedConnection = null)
        {
            // validate it's a valid db connection
            var dbFactory = GetDbFactory(dialect, connectionString);
            if (dbFactory == null)
                throw new ArgumentException("Unable to connect to database", nameof(connectionString));

            var dbConfig = GetDbConfig() ?? new DbConfig();
            if (string.IsNullOrWhiteSpace(namedConnection))
            {
                dbConfig.Dialect = dialect;
                dbConfig.ConnectionString = connectionString;
            }
            else
            {
                if (dbConfig.NamedConnections == null)
                {
                    dbConfig.NamedConnections = new Dictionary<string, DbConnectionDefinition>();
                }
                dbConfig.NamedConnections.Add(namedConnection,
                    new DbConnectionDefinition { Dialect = dialect, ConnectionString = connectionString });
            }

            Directory.CreateDirectory(DataDir);
            var configFile = Path.Combine(DataDir, "db.config.json");
            File.WriteAllText(configFile, dbConfig.ToJson());
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

    public class DbConfig: DbConnectionDefinition
    {
        public Dictionary<string, DbConnectionDefinition> NamedConnections { get; set; }
    }

    public class DbConnectionDefinition
    {
        public string Dialect { get; set; }
        public string ConnectionString { get; set; }
    }
}