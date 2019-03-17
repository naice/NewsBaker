using NETStandard.RestServer;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NewsBaker
{
    internal class ConfigApiResponse
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
    }

    [RestServerServiceInstance(RestServerServiceInstanceType.Instance)]
    internal class ConfigApiService : RestServerService
    {
        private readonly ConfigurationStorage _config;
        private readonly ncoded.NetStandard.Log.ILogger _logger;
        private readonly IJsonConverter _jsonConverter;

        public ConfigApiService(ConfigurationStorage configurationStorage, ncoded.NetStandard.Log.ILogger logger, IJsonConverter jsonConverter)
        {
            _config = configurationStorage ?? throw new ArgumentNullException(nameof(configurationStorage));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jsonConverter = jsonConverter ?? throw new ArgumentNullException(nameof(jsonConverter));
        }

        [RestServerServiceCall("/getconfig")]
        public async Task<Configuration> GetConfig()
        {
            await _config.Open();
            return _config.Content;
        }

        [RestServerServiceCall("/setconfig")]
        public async Task<ConfigApiResponse> SetConfig(string body)
        {
            var response = new ConfigApiResponse() { IsSuccess = true, Message = "Success" };
            try
            {
                var config = _jsonConverter.DeserializeObject<Configuration>(body);
                await _config.Replace(config);
                if (!await _config.Save())
                {
                    response.IsSuccess = false;
                    response.Message = "Could not save!";
                }
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.ToString();
                _logger.Exception(ex);
            }

            return response;
        }
    }
}
