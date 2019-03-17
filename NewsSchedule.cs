using ncoded.NetStandard;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using ncoded.NetStandard.Log;
using System.Threading.Tasks;

namespace NewsBaker
{
    class NewsSchedule : Schedule
    {
        private static readonly TimeSpan MIN_FETCH_DELAY = TimeSpan.FromMinutes(5);


        private readonly ConfigurationStorage _config;
        private readonly NewsFactory _newsFactory;
        private readonly INewsSender _newsSender;
        private readonly ILogger _logger;
        public NewsSchedule(NewsFactory newsFactory, ConfigurationStorage config, INewsSender newsSender, ILogger logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _newsSender = newsSender ?? throw new ArgumentNullException(nameof(newsSender));
            _newsFactory = newsFactory ?? throw new ArgumentNullException(nameof(newsFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Action = ()=> RunSchedule().Wait();
            SetRecurrence(config.Content.NewsFetchDelay);
        }

        private void SetRecurrence(TimeSpan recurrence)
        {
            Recurrence = recurrence < MIN_FETCH_DELAY ? MIN_FETCH_DELAY : recurrence;
        }

        private async Task UpdateRecurrence()
        {
            await _config.Perform(config => SetRecurrence(config.NewsFetchDelay));
        }

        private async Task RunSchedule()
        {
            await UpdateRecurrence();
            var newsGroups = await _newsFactory.GetNews();
            await _newsFactory.FilterKnownNews(newsGroups);
            var newsList = newsGroups.SelectMany(group => group.NewsItems);
            if (!newsList.Any())
            {
                _logger.Info($"No news left after filtering.");
                return;
            }

            foreach (var news in newsList)
            {
                if (string.IsNullOrEmpty(news.ArticleUrl))
                {
                    _logger.Warn($"No article url for {news.Title ?? news.Id}.");
                    continue;
                }
                var meta = MetaScraper.GetMetaDataFromUrl(news.ArticleUrl);
                if (!meta.HasData)
                {
                    _logger.Warn($"Could not fetch metadata from {news.ArticleUrl}.");
                    continue;
                }
                if (string.IsNullOrEmpty(meta.ImageUrl) ||
                    string.IsNullOrEmpty(meta.Title) ||
                    string.IsNullOrEmpty(meta.Description) ||
                    string.IsNullOrEmpty(meta.Url))
                {
                    _logger.Warn($"No enough metadata fetched from  {news.ArticleUrl}.");
                    continue;
                }

                _newsSender.Send(meta);
            }
        }
    }
}
