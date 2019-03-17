using ncoded.NetStandard.Storage;
using System;
using System.IO;
using System.Threading.Tasks;

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
}
