﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.FluentValidation;
using ServiceStack.OrmLite;

namespace WebApp.Plugins.Settings
{
    public class SettingsService: Service
    {
        public IApplicationLifetime AppLifetime { get; set; }
        public IAppSettings AppSettings { get; set; }

        public object Get(GetDb request)
        {
            if (!ZeroUsersOrIsAdmin()) return null;

            return new ApiResponse<Actions.DbConfig>
            {
                Result = Actions.DbAction.GetDbConfig()
            };
        }

        public void Post(SetDb request)
        {
            if (!ZeroUsersOrIsAdmin()) return;

            Actions.DbAction.SetDbConfig(request.Dialect, request.ConnectionString, request.NamedConnection);
        }

        public void Delete(RemoveDb request)
        {
            if (!ZeroUsersOrIsAdmin()) return;

            Actions.DbAction.ClearDbConfig(request.NamedConnection);
        }

        public object Get(GetSettingKeys request)
        {
            if (!ZeroUsersOrIsAdmin()) return null;

            var keys = AppSettings.GetAllKeys().OrderBy(k => k).ToArray();
            return new ApiResponse<string[]>
            {
                Result = keys
            };
        }


        public object Get(GetSetting request)
        {
            if (!ZeroUsersOrIsAdmin()) return null;

            return new ApiResponse<object>
            {
                Result = AppSettings.GetNullableString(request.Key)
            };
        }

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
                    RequiredRoleAttribute.AssertRequiredRoles(Request, RoleNames.Admin);
                    return false;
                }
            }
            return true;
        }
    }

    [Route("/config/db", "GET")]
    public class GetDb: IReturn<ApiResponse<Actions.DbConfig>>
    {
    }

    [Route("/config/db", "POST")]
    [Route("/config/db/{NamedConnection}", "POST")]
    public class SetDb: IReturnVoid
    {
        public string NamedConnection { get; set; }
        public string Dialect { get; set; }
        public string ConnectionString { get; set; }
    }

    [Route("/config/db", "DELETE")]
    [Route("/config/db/{NamedConnection}", "DELETE")]
    public class RemoveDb: IReturnVoid
    {
        public string NamedConnection { get; set; }
    }

    public class SetDbValidator: AbstractValidator<SetDb>
    {
        public SetDbValidator()
        {
            RuleFor(r => r.Dialect).NotEmpty();
            RuleFor(r => r.ConnectionString).NotEmpty();
        }
    }

    [Route("/config/settings", "GET")]
    public class GetSettingKeys: IReturn<ApiResponse<string[]>>
    {
    }

    [Route("/config/settings/{Key}", "GET")]
    public class GetSetting: IReturn<ApiResponse<string>>
    {
        public string Key { get; set; }
    }

    public class GetSettingValidator: AbstractValidator<GetSetting>
    {
        public GetSettingValidator()
        {
            RuleFor(r => r.Key).NotEmpty();
        }
    }

    [Route("/config/settings", "POST")]
    [Route("/config/settings/{Key}", "POST")]
    public class SetSetting: IReturnVoid
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public Dictionary<string, string> Pairs { get; set; }
    }

    [Route("/config/restart", "POST")]
    public class Restart: IReturnVoid
    {

    }
}
