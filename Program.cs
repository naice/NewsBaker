using ncoded.NetStandard.Converter;
using ncoded.NetStandard.Storage;
using ncoded.NetStandard.InversionOfControl;
using ncoded.NetStandard;
using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Net;
using ncoded.NetStandard.Log;
using System.Reflection;

namespace NewsBaker
{
    class StorageIO : IStorageIO
    {
        public Task<string> ReadAllTextAsync(string name)
        {
            return Task.Factory.StartNew<string>(() =>
            {
                try
                {
                    if (File.Exists(name))
                        return File.ReadAllText(name);
                }
                catch (Exception)
                {
                    //log?
                }
                return null;
            });
        }

        public Task WriteAllTextAsync(string name, string text)
        {
            return File.WriteAllTextAsync(name, text);
        }
    }
    interface IJsonConverter : IConverter { }
    class JsonConverter : IJsonConverter
    {
        public T DeserializeObject<T>(string raw) where T : class
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(raw);
        }

        public string SerializeObject(object obj)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented);
        }
    }
    class ConfigurationStorage : Storage<Configuration>
    {
        public ConfigurationStorage(IStorageIO storageIO, IConverter converter, RunTimeInformation runTimeInfo) 
            : base(Path.Combine(runTimeInfo.StartUpPath, "config.json"), storageIO, converter)
        {
        }
    }
    class RunTimeInformation
    {
        public string CurrentDir { get; set; }
        public string StartUp { get; set; }
        public string StartUpPath { get; set; }

        public RunTimeInformation()
        {
            CurrentDir = Environment.CurrentDirectory;
            var exeAssm = System.Reflection.Assembly.GetExecutingAssembly();
            StartUp = exeAssm.Location;
            StartUpPath = Path.GetDirectoryName(StartUp);
        }
    }
    class RestServerIoCWrapper : NETStandard.RestServer.IRestServerServiceDependencyResolver
    {
        private readonly IDependencyResolver _container;
        public RestServerIoCWrapper(IDependencyResolver container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        public object[] GetDependecys(Type[] dependencyTypes)
        {
            return dependencyTypes.Select(type => GetDependency(type)).ToArray();
        }

        public object GetDependency(Type dependencyType)
        {
            return _container.GetDependency(dependencyType);
        }
    }
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


            var meta = MetaScraper.GetMetaDataFromUrl("https://www.derwesten.de/panorama/netflix-doku-ueber-maddie-mccann-freundin-macht-schockierende-entdeckung-id216685497.html");

            var restServer = _container.GetDependency<RestServer>();
            restServer.RegisterRouteHandler(new NETStandard.RestServer.RestServerServiceFileRouteHandler("InetPub"));
            restServer.Start();

            while (true) Task.Delay(100).Wait();
        }
    }
}
