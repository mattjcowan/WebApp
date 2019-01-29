using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace WebApp.Auth
{
    public class AppAuthRepository : OrmLiteAuthRepository
    {
        public AppAuthRepository(IDbConnectionFactory dbFactory) : base(dbFactory)
        {
        }

        public AppAuthRepository(IDbConnectionFactory dbFactory, string namedConnnection = null) : base(dbFactory, namedConnnection)
        {
        }

        public override IUserAuth CreateUserAuth(IUserAuth newUser, string password)
        {
            var userAuth = base.CreateUserAuth(newUser, password);
            if (userAuth != null)
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
            return userAuth;
        }
    }
}