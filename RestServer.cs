using System.Net;
using System.Reflection;

namespace NewsBaker
{
    class RestServer : NETStandard.RestServer.RestServer
    {
        public RestServer(ConfigurationStorage config, RestServerIoCWrapper ioWrapper) 
            : base(
                  new IPEndPoint(IPAddress.Parse(config.Content.IPAddress), config.Content.Port),
                  ioWrapper,
                  Assembly.GetExecutingAssembly())
        {
        }
    }
}
