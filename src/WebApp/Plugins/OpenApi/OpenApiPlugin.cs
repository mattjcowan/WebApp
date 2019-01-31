using Funq;
using ServiceStack;
using ServiceStack.Api.OpenApi;

namespace WebApp.Plugins.OpenApi
{
    public class OpenApiPlugin : IPlugin, ICanPreRegister
    {
        public int PreRegistrationPriority => int.MaxValue;

        public void PreRegister(AppHostBase appHost, Container container)
        {
          appHost.Plugins.Add(new OpenApiFeature());
        }

        public void Register(IAppHost appHost)
        {
        }
    }
}
