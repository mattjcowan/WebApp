using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Funq;
using Microsoft.AspNetCore.Hosting;
using ServiceStack;
using ServiceStack.Api.OpenApi;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Data;

namespace WebApp
{
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
            DbFactory = dbConnectionFactory;
            AppSettings = appSettings;
            DynamicPlugins = dynamicPlugins?.ToList() ?? new List<IPlugin>();
        }

        public IHostingEnvironment HostingEnvironment { get; }
        public IDbConnectionFactory DbFactory { get; }
        public List<IPlugin> DynamicPlugins { get; }

        public override void Configure(Container container)
        {
            var authProviders = new List<IAuthProvider>();
            authProviders.Add(new CredentialsAuthProvider(AppSettings));
            authProviders.Add(new BasicAuthProvider(AppSettings));
            
            var privateKeyXml = (AppSettings as OrmLiteAppSettings)?.GetOrCreate("PrivateKeyXml", () => {
                return RsaUtils.CreatePrivateKeyParams().ToPrivateKeyXml();
            });
            if (!string.IsNullOrWhiteSpace(privateKeyXml))
            {
                authProviders.Add(new JwtAuthProvider(AppSettings) { PrivateKeyXml = privateKeyXml });
            }

            var authFeature = new AuthFeature(() => new AppUserSession(), authProviders.ToArray());
            Plugins.Add(authFeature);

            DynamicPlugins.Each(plugin => {
                Plugins.Add(plugin);
            });
            
            Plugins.Add(new OpenApiFeature());
        }
    }

    public class AppUserSession: AuthUserSession
    {

    }
}