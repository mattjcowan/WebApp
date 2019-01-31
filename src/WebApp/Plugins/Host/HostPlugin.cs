using Funq;
using ServiceStack;

namespace WebApp.Plugins.Host
{
    public class HostPlugin : IPlugin, ICanPreRegister
    {
        public int PreRegistrationPriority => -2;

        public void PreRegister(AppHostBase appHost, Container container)
        {
          var hostConfig = new HostConfig();

          appHost.SetConfig(hostConfig);
        }

        public void Register(IAppHost appHost)
        {
        }
    }
}
