using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Echoer.Models;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static DSharpPlus.Entities.DiscordEmbedBuilder;
using DSharpPlus.CommandsNext;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Echoer
{
    internal class Bot
    {
        private Config Config { get; set; }
        private DiscordClient Client { get; set; }
        private CommandsNextExtension CommandsNext { get; set; }
        private RingBuffer<ulong> EchoedCache { get; set; }
        private DiscordChannel EchoChannel { get; set; }
        private DiscordChannel ArtChannel { get; set; }

        internal async Task StartAsync()
        {
            // initialize the discord client
            await InitaliazeClientAsync();
            Client.DebugLogger.LogMessage(LogLevel.Info, "Bot", "Initializing", DateTime.Now);
         
            // initialize the echo cache and specify its size
            await LoadCache();
            SetUpCommands();

            // cache channels
            EchoChannel = await Client.GetChannelAsync(Config.EchoChannelID);
            ArtChannel = await Client.GetChannelAsync(Config.ArtChannelID);

            // hook events
            Client.MessageReactionAdded += Client_MessageReactionAddedAsync;
            Client.Ready += Client_ReadyAsync;

            Client.DebugLogger.LogMessage(LogLevel.Info, "Bot", "Connecting", DateTime.Now);
            
            // connect the client
            await Client.ConnectAsync().ConfigureAwait(false);

            Client.DebugLogger.LogMessage(LogLevel.Info, "Bot", "Connected", DateTime.Now);

            // block 
            await Task.Delay(-1);
        }


        private void SetUpCommands()
        {
            var deps = new ServiceCollection().AddSingleton(Config);

            var cncfg = new CommandsNextConfiguration
            {
                StringPrefixes = Config.Prefix,
                EnableDms = true,
                EnableMentionPrefix = true,
                Services = deps.BuildServiceProvider()
            };

            CommandsNext = Client.UseCommandsNext(cncfg);
            CommandsNext.RegisterCommands(typeof(Bot).GetTypeInfo().Assembly);
        }

        private async Task Client_ReadyAsync(ReadyEventArgs e)
        {
            Client.DebugLogger.LogMessage(LogLevel.Info, "Bot", "Ready!", DateTime.Now);

            await Client.UpdateStatusAsync(new DiscordActivity(Config.Status, ActivityType.Watching));
        }

        private async Task Client_MessageReactionAddedAsync(MessageReactionAddEventArgs e)
        {
            try
            {
                if (e.Channel.Id != ArtChannel.Id)
                    return;

                // check to see if this is the emote we echo to.
                if (!Config.ReactionEmojiIDs.Contains(e.Emoji.Id))
                    return;

                // get the memver and the perms
                var member = await e.Channel.Guild.GetMemberAsync(e.User.Id);
                var msg = await e.Channel.GetMessageAsync(e.Message.Id);
                var perms = member.PermissionsIn(EchoChannel);

                // check if the member has the needed permissions
                if (!perms.HasPermission(Config.NeededPerm))
                    return;

                // check if this message has already been cached
                if (EchoedCache.Any(ec => ec == e.Message.Id))
                    return;

                var imageUrl = "";
                if (msg.Attachments.Count > 0)
                    if (msg.Attachments[0].Width != 0)
                        imageUrl = msg.Attachments[0].Url;

                // shouldn't need this but just incase.
                if (imageUrl == null)
                    imageUrl = "";

                DiscordMember artPoster = null;

                try
                {
                    artPoster = await ArtChannel.Guild.GetMemberAsync(msg.Author.Id);
                }
                catch (Exception exc)
                {
                    new LogWriter($"{exc.Message}\n{exc.StackTrace}\n\nAuthor ID:{msg.Author.Id}");
                    Client.DebugLogger.LogMessage(LogLevel.Error, "Bot", exc.Message, DateTime.Now);
                    Client.DebugLogger.LogMessage(LogLevel.Error, "Bot", "That user is nolonger present in this server", DateTime.Now);
                    return;
                }


                // if we dont have attachments check the the message for links
                if (msg.Attachments.Count == 0)
                {
                    MatchCollection ms = Regex.Matches(msg.Content, @"(www.+|http.+)([\s]|$)");
                    if (ms.Count > 0 && IsImageUrl(ms[0].Value.ToString()))
                        imageUrl = ms[0].Value.ToString();
                }

                try
                {
                    switch (imageUrl)
                    {
                        // if theres no attachments just send the message content
                        // TODO: in the future check to see if there are any links and try grabing the images from them
                        //       and display said image in the embed.
                        case "":

                            var eb = new DiscordEmbedBuilder
                            {
                                Title = $"Some amazing art by {artPoster.DisplayName}! ({artPoster.Username}#{artPoster.Discriminator})",
                                Description = msg.Content,
                                Footer = new EmbedFooter
                                {
                                    Text = msg.Timestamp.ToString(),
                                    IconUrl = artPoster.AvatarUrl
                                },
                                Color = new DiscordColor(Config.EmbedColor)
                            };
                            await Client.SendMessageAsync(EchoChannel, "", false, eb);

                            AddToCache(msg.Id);
                            break;

                        // sen a normal embed with a image.
                        default:
                            var ebImage = new DiscordEmbedBuilder
                            {
                                Title = $"Some amazing art by {artPoster.DisplayName}! ({artPoster.Username}#{artPoster.Discriminator})",
                                Description = msg.Content,
                                ImageUrl = imageUrl,
                                Footer = new EmbedFooter
                                {
                                    Text = msg.Timestamp.ToString(),
                                    IconUrl = artPoster.AvatarUrl
                                },
                                Color = new DiscordColor("#" + Config.EmbedColor),
                            };

                            await Client.SendMessageAsync(EchoChannel, "", false, ebImage);

                            // add the messageid to the cache
                            AddToCache(msg.Id);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    new LogWriter($"{ex.Message}\n{ex.StackTrace}");
                    Client.DebugLogger.LogMessage(LogLevel.Error, "Bot", ex.Message, DateTime.Now);
                }
                new LogWriter($"Messaged echoed.\n{msg.Content}\n{imageUrl}");
                Client.DebugLogger.LogMessage(LogLevel.Info, "Bot", $"Messaged echoed.\n{msg.Content}\n{imageUrl}", DateTime.Now);
            }
            catch (Exception exc)
            {
                new LogWriter($"{exc.Message}\n{exc.StackTrace}");
                Client.DebugLogger.LogMessage(LogLevel.Error, "Bot", exc.Message, DateTime.Now);
            }
        }

        /// <summary>
        /// Adds a message id to cache and saves the cache
        /// </summary>
        /// <param name="msgId"></param>
        private void AddToCache(ulong msgId)
        {
            EchoedCache.Add(msgId);
            SaveCache();
        }


        /// <summary>
        /// Initializes the DiscordClient.
        /// </summary>
        /// <returns></returns>
        private async Task InitaliazeClientAsync()
        {
            Config = await LoadConfigAsync();
            var config = new DiscordConfiguration
            {
                AutoReconnect = true,

                LargeThreshold = 250,
                LogLevel = LogLevel.Debug,
                UseInternalLogHandler = true,

                MessageCacheSize = 2048,


                Token = Config.Token,
                TokenType = TokenType.Bot
            };

            Client = new DiscordClient(config);
        }

        /// <summary>
        /// Loads the bots configuration from file.
        /// </summary>
        /// <returns></returns>
        private async Task<Config> LoadConfigAsync()
        {
            var json = "{}";
            var utf8 = new UTF8Encoding(false);
            var fi = new FileInfo("config.json");
            if (!fi.Exists)
            {
                Console.WriteLine("loading Config failed.");

                json = JsonConvert.SerializeObject(Config.DefualtConfig, Formatting.Indented);
                using (var fs = fi.Create())
                using (var sw = new StreamWriter(fs, utf8))
                {
                    await sw.WriteAsync(json);
                    await sw.FlushAsync();
                }
                Console.WriteLine($"New settings file generated at '{fi.FullName}'\nPlease edit 'config.json' and then relaunch.");
                Console.ReadLine();
            }

            using (var fs = fi.OpenRead())
            using (var sr = new StreamReader(fs, utf8))
                json = await sr.ReadToEndAsync();

            return JsonConvert.DeserializeObject<Config>(json);
        }

        bool IsImageUrl(string URL)
        {
            try
            {
                var req = (HttpWebRequest)HttpWebRequest.Create(URL);
                req.Method = "HEAD";
                using (var resp = req.GetResponse())
                {
                    return resp.ContentType.ToLower(CultureInfo.InvariantCulture)
                               .StartsWith("image/");
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Save cache
        /// </summary>
        /// <returns></returns>
        public void SaveCache()
        {
            var json = JsonConvert.SerializeObject(EchoedCache, Formatting.Indented);
            File.WriteAllText("cache", json);
            return;
        }

        /// <summary>
        /// Loads cache from file.
        /// </summary>
        /// <returns></returns>
        public async Task LoadCache()
        {
            var json = "{}";
            var utf8 = new UTF8Encoding(false);
            var fi = new FileInfo("cache");

            // if the file doesnt exist return a empty cache.
            if (!fi.Exists)
            {
                EchoedCache = new RingBuffer<ulong>(Config.EchoedCache);
                return;
            }

            using (var fs = fi.OpenRead())
            using (var sr = new StreamReader(fs, utf8))
                json = await sr.ReadToEndAsync();

            var tmpCache = JsonConvert.DeserializeObject<RingBuffer<ulong>>(json);
            if (tmpCache.Count == Config.EchoedCache)
                EchoedCache = tmpCache;
            else
            {
                EchoedCache = new RingBuffer<ulong>(Config.EchoedCache);
                foreach (var i in tmpCache)
                {
                    if (i != 0)
                        EchoedCache.Add(i);
                }
            }
            return;
        }
    }
}