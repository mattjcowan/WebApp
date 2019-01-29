using System;
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
using WebApp.Auth;

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
            SetConfig(new HostConfig
            {
                HandlerFactoryPath = "api"
            });

            ConfigureAuth();

            DynamicPlugins.Each(plugin => {
                Plugins.Add(plugin);
            });
            
            ConfigureOpenApi();
        }
        
        private void ConfigureAuth()
        {
            var authProviders = new List<IAuthProvider>();
            authProviders.Add(new CredentialsAuthProvider(AppSettings));
            authProviders.Add(new BasicAuthProvider(AppSettings));
            authProviders.Add(new ApiKeyAuthProvider(AppSettings) { RequireSecureConnection = false });
            
            var privateKeyXml = (AppSettings as OrmLiteAppSettings)?.GetOrCreate("PrivateKeyXml", () => {
                return RsaUtils.CreatePrivateKeyParams().ToPrivateKeyXml();
            });
            if (!string.IsNullOrWhiteSpace(privateKeyXml))
            {
                authProviders.Add(new JwtAuthProvider(AppSettings) 
                { 
                    PrivateKeyXml = privateKeyXml,
                    RequireSecureConnection = false,
                    SetBearerTokenOnAuthenticateResponse = true,
                    IncludeJwtInConvertSessionToTokenResponse = true
                });
            }

            var authRepository = new OrmLiteAuthRepository(DbFactory);
            authRepository.InitSchema();
            authRepository.InitApiKeySchema();
            Register<IUserAuthRepository>(authRepository);
            Register<IAuthRepository>(authRepository);

            var authFeature = new AuthFeature(() => new AppUserSession(), authProviders.ToArray())
            {
                IncludeRegistrationService = false,
                IncludeAssignRoleServices = false,
                DeleteSessionCookiesOnLogout = true,
                GenerateNewSessionCookiesOnAuthentication = true,
                SaveUserNamesInLowerCase = true,
                ValidateUniqueEmails = true,
                ValidateUniqueUserNames = true
            };
            
            authFeature.ServiceRoutes[typeof(AuthenticateService)] =
                authFeature.ServiceRoutes[typeof(AuthenticateService)].Where(r => 
                !r.Contains("authenticate")).ToArray();
                
            Plugins.Add(authFeature);
        }
    
        private void ConfigureOpenApi()
        {
            Plugins.Add(new OpenApiFeature());
        }
    }
}