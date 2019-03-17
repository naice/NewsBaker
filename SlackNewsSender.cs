using ncoded.NetStandard.Log;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace NewsBaker
{
    class SlackMessage
    {
        public List<SlackAttachment> attachments { get; set; } = new List<SlackAttachment>();
    }
    class SlackAttachment
    {
        public string fallback { get; set; }
        public string title { get; set; }
        public string title_link { get; set; }
        public string text { get; set; }
        public string image_url { get; set; }
    }
    class SlackNewsSender : INewsSender
    {
        private readonly ConfigurationStorage _config;
        private readonly IJsonConverter _jsonConverter;
        private readonly ILogger _logger;

        public SlackNewsSender(ConfigurationStorage config, IJsonConverter jsonConverter, ILogger logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _jsonConverter = jsonConverter ?? throw new ArgumentNullException(nameof(jsonConverter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Send(MetaInformation meta)
        {
            _logger.Info($"Sending news {meta.Title??"NOTITLE"}");
            try
            {
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(_config.Content.SlackWebHook);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    var slackMessage = new SlackMessage()
                    {
                        attachments = new List<SlackAttachment>()
                        {
                            new SlackAttachment()
                            {
                                fallback = meta.Description,
                                title = meta.Title,
                                title_link = meta.Url,
                                text = meta.Description,
                                image_url = meta.ImageUrl,
                            },
                        }
                    };
                    var json = _jsonConverter.SerializeObject(slackMessage);
                    streamWriter.Write(json);
                    streamWriter.Flush();
                    streamWriter.Close();
                }

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex);
            }
        }
    }
}
