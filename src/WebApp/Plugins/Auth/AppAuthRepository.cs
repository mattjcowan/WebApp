using System;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace WebApp.Plugins.Auth
{
    public class AppAuthRepository : OrmLiteAuthRepository
    {
        public AppAuthRepository(IDbConnectionFactory dbFactory) : base(dbFactory)
        {
        }

        public AppAuthRepository(IDbConnectionFactory dbFactory, string namedConnnection = null) : base(dbFactory, namedConnnection)
        {
        }

        public override bool TryAuthenticate(string userName, string password, out IUserAuth userAuth)
        {
            var authenticated = base.TryAuthenticate(userName, password, out userAuth);
            if (authenticated)
            {
                var userAuthId = userAuth.Id;
                using(var db = base.OpenDbConnection())
                {
                    db.Update<UserAuth>(new { LastLoginAttempt = DateTime.UtcNow, InvalidLoginAttempts = 0 }, u => u.Id == userAuthId);
                }

                if (!userAuth.Roles.Contains(RoleNames.Admin))
                {
                    MakeAdminIfSingleUser(userAuth);
                    authenticated = base.TryAuthenticate(userName, password, out userAuth);
                }
            }
            return authenticated;
        }

        public override IUserAuth CreateUserAuth(IUserAuth newUser, string password)
        {
            var userAuth = base.CreateUserAuth(newUser, password);
            if (userAuth != null)
            {
                MakeAdminIfSingleUser(userAuth);
            }
            return userAuth;
        }

        private void MakeAdminIfSingleUser(IUserAuth userAuth)
        {
            long userCount = 0;
            using(var db = base.OpenDbConnection())
            {
                userCount = db.Count<UserAuth>();
            }
            if (userCount == 1)
            {
                this.AssignRoles(userAuth, new string[] { RoleNames.Admin });
            }
        }
    }
}
