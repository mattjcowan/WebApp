using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.OrmLite;

namespace WebApp.Settings
{
    public class SettingsService: Service
    {
        public IApplicationLifetime AppLifetime { get; set; }
        public IAppSettings AppSettings { get; set; }

        public void Post(SetSetting request)
        {
            if (!ZeroUsersOrIsAdmin()) return;

            if (!string.IsNullOrWhiteSpace(request.Key))
            {
                AppSettings?.Set(request.Key, request.Value);            
            }

            if (request.Pairs != null)
            {
                request.Pairs.Each(pair => {
                    if (!string.IsNullOrWhiteSpace(pair.Key))
                    {
                        AppSettings?.Set(pair.Key, pair.Value);            
                    }
                });
            }
        }

        public void Post(Restart request)
        {
            if (!ZeroUsersOrIsAdmin()) return;

            AppLifetime?.StopApplication();
        }

        private bool ZeroUsersOrIsAdmin()
        {
            // can only make changes while there are no users in the system
            // OR user is authenticated and is an admin
            var userCount = Db.Count<UserAuth>();
            if (userCount > 0)
            {
                if (!base.IsAuthenticated)
                {
                    new AuthenticateAttribute().ExecuteAsync(base.Request, base.Response, null).Wait();
                    return false;
                }
                else if (!base.GetSession().HasRole(RoleNames.Admin, base.AuthRepository))
                {
                    new RequiredRoleAttribute(RoleNames.Admin).ExecuteAsync(base.Request, base.Response, null).Wait();
                    return false;
                }
            }
            return true;
        }
    }

    [Route("/settings", "POST")]
    public class SetSetting: IReturnVoid
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public Dictionary<string, string> Pairs { get; set; }
    }

    [Route("/restart", "POST")]
    public class Restart: IReturnVoid
    {

    }
}