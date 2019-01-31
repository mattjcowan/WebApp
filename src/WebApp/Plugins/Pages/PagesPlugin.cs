using Funq;
using ServiceStack;

namespace WebApp.Plugins.Pages
{
    public class PagesPlugin : IPlugin
    {
        public int PreRegistrationPriority => 0;

        public void Register(IAppHost appHost)
        {
        }
    }
}
