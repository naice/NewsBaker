using Microsoft.SyndicationFeed;
using Microsoft.SyndicationFeed.Rss;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using ncoded.NetStandard.Log;

namespace NewsBaker
{
    class NewsGroup
    {
        public string Name { get; set; }
        public List<NewsItem> NewsItems { get; set; } = new List<NewsItem>();
    }
    class NewsItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }
        public List<string> Links { get; set; } = new List<string>();
        public string ArticleUrl { get; set; }
        public DateTime Date { get; set; }
    }

    class NewsFactory
    {
        private readonly ConfigurationStorage _configStore;
        private readonly ILogger _logger;

        public NewsFactory(ConfigurationStorage configStore, ILogger logger)
        {
            _configStore = configStore ?? throw new ArgumentNullException(nameof(configStore));
            _logger = logger;
        }

        public async Task<List<NewsGroup>> GetNews()
        {
            var newsGroups = new List<NewsGroup>();
            foreach (var feed in _configStore.Content.RssFeeds)
            {
                _logger?.Info($"Building news group {feed.Name}");
                var newsGroup = new NewsGroup
                {
                    Name = feed.Name
                };
                try
                {
                    using (var xmlReader = XmlReader.Create(feed.Url, new XmlReaderSettings() { Async = true, XmlResolver = new XmlUrlResolver() }))
                    {
                        var feedReader = new RssFeedReader(xmlReader);
                        while (await feedReader.Read())
                        {
                            switch (feedReader.ElementType)
                            {
                                // Read category
                                case SyndicationElementType.Category:
                                    ISyndicationCategory category = await feedReader.ReadCategory();
                                    break;

                                // Read Image
                                case SyndicationElementType.Image:
                                    ISyndicationImage image = await feedReader.ReadImage();
                                    break;

                                // Read Item
                                case SyndicationElementType.Item:
                                    ISyndicationItem item = await feedReader.ReadItem();
                                    newsGroup.NewsItems.Add(new NewsItem() {
                                        Id = CalculateMD5Hash(newsGroup.Name + item.Title + item.Description),
                                        Description = item.Description,
                                        Title = item.Title,
                                        Links = item.Links.Select(lnk => lnk.Uri.AbsoluteUri).ToList(),
                                        ArticleUrl = item.Links.Where(lnk => lnk.RelationshipType == "alternate").Select(lnk => lnk.Uri.AbsoluteUri).FirstOrDefault() ??
                                                     item.Links.FirstOrDefault()?.Uri?.AbsoluteUri,
                                        Date = item.Published.UtcDateTime == DateTime.MinValue ? DateTime.UtcNow : item.Published.UtcDateTime,
                                    });
                                    break;

                                // Read link
                                case SyndicationElementType.Link:
                                    ISyndicationLink link = await feedReader.ReadLink();
                                    break;

                                // Read Person
                                case SyndicationElementType.Person:
                                    ISyndicationPerson person = await feedReader.ReadPerson();
                                    break;

                                // Read content
                                default:
                                    ISyndicationContent content = await feedReader.ReadContent();
                                    break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.Exception(ex);
                }
                _logger?.Info($"Found {newsGroup.NewsItems.Count} for {newsGroup.Name}");
                newsGroup.NewsItems.Sort((a, b) => a.Date.CompareTo(b.Date));
                newsGroups.Add(newsGroup);
            }

            return newsGroups;
        }

        public async Task FilterKnownNews(List<NewsGroup> groups)
        {
            var isSaveNeeded = false;
            await _configStore.Perform(config => {

                // remove all hashes older as the news age offset.
                var hashCountInitial = config.RssFeedHashes.Count;
                config.RssFeedHashes.RemoveAll(hashInfo => hashInfo.Created <= DateTime.UtcNow - config.NewsAgeOffset);
                var hashCountAfter = config.RssFeedHashes.Count;
                if (hashCountAfter != hashCountInitial)
                    isSaveNeeded = true;

                foreach (var group in groups)
                {
                    var filteredItemsCount = 0;
                    _logger?.Info($"Filtering {group.Name}.");
                    foreach (var news in group.NewsItems.ToList())
                    {
                        // ignore very old news
                        if (news.Date <= DateTime.UtcNow - config.NewsAgeOffset)
                        {
                            filteredItemsCount++;
                            group.NewsItems.Remove(news);
                            continue;
                        }

                        var rssHash = new RSSFeedHashInfo()
                        {
                            Hash = news.Id,
                            Created = news.Date == DateTime.MinValue ? DateTime.UtcNow : news.Date
                        };
                        
                        // ignore known news
                        if (config.RssFeedHashes.Any(item => item.Hash == rssHash.Hash))
                        {
                            filteredItemsCount++;
                            group.NewsItems.Remove(news);
                            continue;
                        }

                        // remember seen news.
                        config.RssFeedHashes.Add(rssHash);
                        isSaveNeeded = true;
                    }

                    _logger?.Info($"Filtered {filteredItemsCount} for {group.Name}.");
                }
            });
            if (isSaveNeeded)
                await _configStore.Save();
        }

        private string CalculateMD5Hash(string input)
        {
            // step 1, calculate MD5 hash from input
            System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }
    }
}
