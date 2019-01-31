using Funq;
using ServiceStack;
using ServiceStack.Api.OpenApi;
using ServiceStack.Configuration;
using ServiceStack.Data;

namespace WebApp.Plugins.Settings
{
    public class SettingsPlugin : IPlugin, ICanPreRegister
    {
        public int PreRegistrationPriority => int.MinValue;

        public void PreRegister(AppHostBase appHost, Container container)
        {
            if (appHost.AppSettings == null)
              return; // missing required dependencies

            if (container.TryResolve<IAppSettings>() == null)
              container.Register<IAppSettings>(appHost.AppSettings);
        }

        public void Register(IAppHost appHost)
        {
        }
    }
}
