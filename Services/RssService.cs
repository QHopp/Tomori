using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using AngleSharp;
using AngleSharp.Html.Dom;
using DSharpPlus.Entities;

namespace Tomori.Services
{
    public class RssService
    {
        private readonly HttpClient _client;

        public RssService(HttpClient client)
        {
            _client = client;
            _client.DefaultRequestHeaders.Add("User-Agent", "C# App");
        }

        public async Task<IEnumerable<SyndicationItem>?> GetRssByUriLogAsync(string uri, DateTimeOffset date,
            string pathToLogDate)
        {
            using var reader = XmlReader.Create(uri);
            SyndicationFeed feed = SyndicationFeed.Load(reader);
            var filtered = feed.Items.Where(item => item.PublishDate > date);
            if (!filtered.Any())
                return null;
            else
            {
                try
                {
                    await File.WriteAllTextAsync(pathToLogDate, filtered.First().PublishDate.ToString());
                }
                catch
                {
                    await File.WriteAllTextAsync(pathToLogDate, DateTimeOffset.Now.ToString());
                }

                return filtered;
            }
        }

        public async Task<string?> FetchFaviconAsync(string url)
        {
            try
            {
                var html = await _client.GetStringAsync(url);

                using var context = BrowsingContext.New();
                using var document = await context.OpenAsync(req =>
                {
                    req.Content(html);
                    req.Address(url);
                });
                var element =
                    document.QuerySelector(
                            "link[rel=icon][href$=\".png\"], meta[property=og\\:image][content$=\".png\"]") as
                        IHtmlLinkElement;
                return element?.Href;
            }
            catch
            {
                return null;
            }
        }

        public async Task<IEnumerable<DiscordChannel>> GetGuildRssUrlsAsync(DiscordGuild discordGuild)
        {
            var channels = await discordGuild.GetChannelsAsync();
            var valids = channels.Where(chann => Uri.IsWellFormedUriString(chann.Topic, UriKind.Absolute));
            var rss = new List<DiscordChannel>(valids.Count());
            foreach (var valid in valids)
            {
                var content = await _client.GetStringAsync(valid.Topic);
                if (content.Contains("</rss>"))
                    rss.Add(valid);
            }

            return rss;
        }

        public IEnumerable<string> GenerateEmoji()
        {
            int hex = 0x1f600;
            for (int i = 0; i < 10; i++, hex++)
            {
                Rune a = new Rune(hex);
                yield return a.ToString();
            }
        }
    }
}