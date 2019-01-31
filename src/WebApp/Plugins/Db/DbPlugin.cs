using Funq;
using ServiceStack;
using ServiceStack.Data;

namespace WebApp.Plugins.Db
{
    public class DbPlugin : IPlugin, ICanPreRegister
    {
        public int PreRegistrationPriority => int.MinValue;

        public void PreRegister(AppHostBase appHost, Container container)
        {
        }

        public void Register(IAppHost appHost)
        {
        }
    }
}
