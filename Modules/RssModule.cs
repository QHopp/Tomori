using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Tomori.Services;

namespace Tomori.Modules
{
    public class RssModule : BaseCommandModule
    {
        private readonly Random _random_color;
        private readonly RssService _service;
        private readonly ConcurrentBag<Timer> _timers;

        public RssModule(RssService service, Random color)
        {
            _service = service;
            _timers = new ConcurrentBag<Timer>();
            _random_color = color;
        }

        [Command("sub")]
        [RequireOwner]
        [Description("Subscribe to every rss feed present in text channel topic")]
        public async Task SubRSS(CommandContext ctx)
        {
            await ctx.RespondAsync("Started");
            var channels = await _service.GetGuildRssUrlsAsync(ctx.Guild);
            foreach (var channel in channels)
            {
                try
                {
                    await channel.SendMessageAsync($"Subscribed to {channel.Topic} at {DateTimeOffset.Now}");
                }
                catch
                {
                    Console.Error.WriteLine("Exception messsage");
                }
            }

            async void Callback(object? s)
            {
                foreach (var channel in channels)
                {
                    try
                    {
                        Uri uri_parsed = new(channel.Topic);
                        var path = $"log_rss/log_{uri_parsed.Host}.txt";
                        IEnumerable<SyndicationItem>? feed;
                        if (File.Exists(path))
                        {
                            var date = await File.ReadAllTextAsync(path);
                            feed = await _service.GetRssByUriLogAsync(uri_parsed.ToString(), DateTimeOffset.Parse(date),
                                path);
                        }
                        else
                        {
                            feed = await _service.GetRssByUriLogAsync(uri_parsed.ToString(),
                                DateTimeOffset.Now.Subtract(TimeSpan.FromDays(3)), path);
                        }

                        if (feed is null) continue;

                        var homepage = $"https://{uri_parsed.Host}";

                        string favicon = await _service.FetchFaviconAsync(homepage) switch
                        {
                            { } fav when Uri.IsWellFormedUriString(fav, UriKind.Absolute) => fav,
                            _ => ctx.Client.CurrentUser.GetAvatarUrl(ImageFormat.Auto),
                        };
                        var author = new DiscordEmbedBuilder.EmbedAuthor()
                        {
                            IconUrl = favicon,
                            Url = homepage,
                            Name = uri_parsed.Host.Substring(0, Math.Min(uri_parsed.Host.Length, 200))
                        };
                        foreach (var item in feed)
                        {
                            DiscordEmbedBuilder embed = new()
                            {
                                Title = item.Title?.Text.Substring(0, Math.Min(item.Title.Text.Length, 200)),
                                Url = item.Links?.FirstOrDefault()?.Uri.ToString() ?? homepage,
                                Description = new ReverseMarkdown.Converter().Convert(
                                    item.Summary?.Text.Substring(0, Math.Min(item.Summary.Text.Length, 1000)) ??
                                    String.Empty),
                                Footer = new DiscordEmbedBuilder.EmbedFooter() {Text = "RSS by Tomori"},
                                Timestamp = DateTimeOffset.Now,
                                Author = author,
                                Color = Optional.FromValue<DiscordColor>(_random_color.Next(16777216)),
                            };
                            await channel.SendMessageAsync(embed: embed.Build());
                        }
                    }
                    catch (Exception e)
                    {
                        DiscordEmbedBuilder embed = new();
                        var built = embed.WithColor(new DiscordColor(255, 0, 0))
                            .WithAuthor(channel.Name, "https://image.prntscr.com/image/1tlt8aj7RY_ywP-OPPivyg.png")
                            .WithFooter("RSS by Tomori")
                            .WithTitle(e.Message)
                            .WithDescription(e.StackTrace)
                            .Build();
                        await ctx.RespondAsync(built);
                    }
                }
            }

            _timers.Add(new Timer(Callback, null, TimeSpan.Zero, TimeSpan.FromMinutes(15)));
        }
    }
}