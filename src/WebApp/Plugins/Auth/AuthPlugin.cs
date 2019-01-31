using System;
using System.Collections.Generic;
using System.Linq;
using Funq;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Data;

namespace WebApp.Plugins.Auth
{
    public class AuthPlugin : IPlugin, ICanPreRegister
    {
        public int PreRegistrationPriority => -1;

        public void PreRegister(AppHostBase appHost, Container container)
        {
            var dbFactory = container.TryResolve<IDbConnectionFactory>();
            var appSettings = container.TryResolve<IAppSettings>();

            if (dbFactory == null || appSettings == null)
              return; // missing required dependencies

            var authProviders = new List<IAuthProvider>();
            authProviders.Add(new CredentialsAuthProvider(appSettings));
            authProviders.Add(new BasicAuthProvider(appSettings));

            var apiKeyProvider = new ApiKeyAuthProvider(appSettings) {
                RequireSecureConnection = false,
                ServiceRoutes = new Dictionary<Type, string[]>
                {
                    { typeof(GetApiKeysService), new[] { "/auth/apikeys", "/auth/apikeys/{Environment}" } },
                    { typeof(RegenerateApiKeysService), new [] { "/auth/apikeys/regenerate", "/auth/apikeys/regenerate/{Environment}" } },
                }
            };
            authProviders.Add(apiKeyProvider);

            var privateKeyXml = (appSettings as OrmLiteAppSettings)?.GetOrCreate("PrivateKeyXml", () => {
                return RsaUtils.CreatePrivateKeyParams().ToPrivateKeyXml();
            });
            if (!string.IsNullOrWhiteSpace(privateKeyXml))
            {
                authProviders.Add(new JwtAuthProvider(appSettings)
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

            var authRepository = new AppAuthRepository(dbFactory);
            authRepository.InitSchema();
            authRepository.InitApiKeySchema();
            appHost.Register<IUserAuthRepository>(authRepository);
            appHost.Register<IAuthRepository>(authRepository);

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

            appHost.Plugins.Add(authFeature);

            var regFeature = new RegistrationFeature
            {
                AllowUpdates = false,
                AtRestPath = "/auth/register"
            };
            appHost.Plugins.Add(regFeature);
        }

        public void Register(IAppHost appHost)
        {
        }
    }
}
