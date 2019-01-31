using Funq;
using ServiceStack;

namespace WebApp.Plugins
{
    public interface ICanPreRegister
    {
      int PreRegistrationPriority { get; }
      void PreRegister(AppHostBase appHost, Container container);
    }
}
