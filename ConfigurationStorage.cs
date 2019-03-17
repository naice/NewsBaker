using ncoded.NetStandard.Converter;
using ncoded.NetStandard.Storage;
using System.IO;

namespace NewsBaker
{
    class ConfigurationStorage : Storage<Configuration>
    {
        public ConfigurationStorage(IStorageIO storageIO, IConverter converter, RunTimeInformation runTimeInfo) 
            : base(Path.Combine(runTimeInfo.StartUpPath, "config.json"), storageIO, converter)
        {
        }
    }
}
