using ServiceStack.Auth;
using ServiceStack.Data;

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
    }
}