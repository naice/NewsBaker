using ncoded.NetStandard.InversionOfControl;
using ncoded.NetStandard;
using System.Threading.Tasks;
using ncoded.NetStandard.Log;

namespace NewsBaker
{
    class Program
    {
        private static readonly Container _container;
        private static readonly ILogger _logger;

        static Program()
        {
            _container = new Container();
            _container.WithSingleton<RunTimeInformation>();
            _container.WithSingleton<ConsoleLogger>();
            _logger = _container.GetDependency<ConsoleLogger>();
            _container.WithSingleton<StorageIO>();
            _container.WithSingleton<Scheduler>();
            _container.WithSingleton<JsonConverter>();
            _container.WithSingleton<ConfigurationStorage>();
            _container.WithSingleton<RestServerIoCWrapper>();
            _container.WithType<NewsFactory>();
            _container.WithType<SlackNewsSender>();
            _container.WithType<NewsSchedule>();
            _container.WithSingleton<RestServer>();
        }

        static void Main(string[] args)
        {
            var runTimeInfo = _container.GetDependency<RunTimeInformation>();
            _logger.Info($"Starting NewsBaker");
            _logger.Info($"Current Dir {runTimeInfo.CurrentDir}");
            _logger.Info($"StartUp {runTimeInfo.StartUp}");
            _logger.Info($"StartUpPath {runTimeInfo.StartUpPath}");

            var config = _container.GetDependency<ConfigurationStorage>();
            _logger.Info($"Configuration Path {config.Name}");
            _logger.Info($"Loading config.");
            config.Open().Wait();
            config.Save().Wait();
            _logger.Info($"Loading config finished.");

            var scheduler = _container.GetDependency<Scheduler>();
            var newsSchedule = _container.GetDependency<NewsSchedule>();
            scheduler.StartSchedule(newsSchedule);

            var restServer = _container.GetDependency<RestServer>();
            restServer.RegisterRouteHandler(new NETStandard.RestServer.RestServerServiceFileRouteHandler("InetPub"));
            restServer.Start();

            while (true) Task.Delay(100).Wait();
        }
    }
}
