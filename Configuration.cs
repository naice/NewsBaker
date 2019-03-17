using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NewsBaker
{
    class RSSFeed
    {
        public string Name { get; set; }
        public string Url { get; set; } 
    }

    class RSSFeedHashInfo
    {
        public string Hash { get; set; }
        public DateTime Created { get; set; }
    }

    class Configuration
    {
        public List<RSSFeed> RssFeeds { get; set; } = new List<RSSFeed>();
        public List<RSSFeedHashInfo> RssFeedHashes { get; set; } = new List<RSSFeedHashInfo>();
        public TimeSpan NewsAgeOffset { get; set; } = TimeSpan.FromDays(15);
        public string SlackWebHook { get; set; } = "SlackHookUrl";
        public TimeSpan NewsFetchDelay { get; set; } = TimeSpan.FromMinutes(15);
        public string IPAddress { get; set; } = GetLocalIPAddress();
        public int Port { get; set; } = 6397;

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "127.0.0.1";
        }
    }
}
