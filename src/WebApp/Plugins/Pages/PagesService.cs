using ServiceStack;

namespace WebApp.Plugins.Pages
{
    public class PagesService: Service
    {
        public object Any(PageRequest request)
        {
            var host = ServiceStackHost.Instance;
            return new
            {
                ServiceName = host.ServiceName,
                ApiVersion = host.Config.ApiVersion,
                StartedAt = host.StartedAt,
                PathInfo = "/" + (request.PathInfo ?? string.Empty),
                Host = Request.GetUrlHostName(),
                RemoteIp = Request.RemoteIp
            };
        }
    }

    [FallbackRoute("/{PathInfo*}")]
    public class PageRequest
    {
        public string PathInfo { get; set; }
    }
}
