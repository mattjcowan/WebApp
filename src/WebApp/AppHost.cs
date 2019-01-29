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

            var apiKeyProvider = new ApiKeyAuthProvider(AppSettings) { 
                RequireSecureConnection = false,
                ServiceRoutes = new Dictionary<Type, string[]>
                {
                    { typeof(GetApiKeysService), new[] { "/auth/apikeys", "/auth/apikeys/{Environment}" } },
                    { typeof(RegenerateApiKeysService), new [] { "/auth/apikeys/regenerate", "/auth/apikeys/regenerate/{Environment}" } },
                }
            };
            authProviders.Add(apiKeyProvider);
            
            var privateKeyXml = (AppSettings as OrmLiteAppSettings)?.GetOrCreate("PrivateKeyXml", () => {
                return RsaUtils.CreatePrivateKeyParams().ToPrivateKeyXml();
            });
            if (!string.IsNullOrWhiteSpace(privateKeyXml))
            {
                authProviders.Add(new JwtAuthProvider(AppSettings) 
                { 
                    PrivateKeyXml = privateKeyXml,
                    HashAlgorithm = "RS256",
                    RequireSecureConnection = false,
                    SetBearerTokenOnAuthenticateResponse = true,
                    IncludeJwtInConvertSessionToTokenResponse = true,
                    ServiceRoutes = new Dictionary<Type, string[]>
                    {
                        { typeof(ConvertSessionToTokenService), new[] { "/auth/session-to-token" } },
                        { typeof(GetAccessTokenService), new[] { "/auth/access-token" } },
                    }
                });
            }

            var authRepository = new AppAuthRepository(DbFactory);
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

            var regFeature = new RegistrationFeature
            {
                AllowUpdates = false,
                AtRestPath = "/auth/register"
            };
            Plugins.Add(regFeature);
        }
    
        private void ConfigureOpenApi()
        {
            Plugins.Add(new OpenApiFeature());
        }
    }
}