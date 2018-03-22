using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Echoer.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DSharpPlus.Entities.DiscordEmbedBuilder;

namespace Echoer
{
    internal class Bot
    {
        private Config Config { get; set; }
        private DiscordClient Client { get; set; }
        private RingBuffer<ulong> EchoedCache { get; set; }
        private DiscordChannel EchoChannel { get; set; }
        private DiscordChannel ArtChannel { get; set; }

        internal async Task StartAsync()
        {

            // initialize the discord client
            await InitaliazeClientAsync();

            // initialize the echo cache and specify its size
            EchoedCache = new RingBuffer<ulong>(Config.EchoedCache);

            // cache channels
            EchoChannel = await Client.GetChannelAsync(Config.EchoChannelID);
            ArtChannel = await Client.GetChannelAsync(Config.ArtChannelID);

            // hook events
            Client.MessageReactionAdded += Client_MessageReactionAddedAsync;
            Client.Ready += Client_ReadyAsync;

            // connect the client
            await Client.ConnectAsync().ConfigureAwait(false);

            // block 
            await Task.Delay(-1);
        }

        private async Task Client_ReadyAsync(ReadyEventArgs e)
        {
            await Client.UpdateStatusAsync(new DiscordActivity(Config.Status, ActivityType.Watching));
        }

        private async Task Client_MessageReactionAddedAsync(MessageReactionAddEventArgs e)
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
            catch
            {
                Console.WriteLine("That user is nolonger present in this server.");
                return;
            }

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
                        Color = new DiscordColor(Config.EmbedColor),
                    };
                    await Client.SendMessageAsync(EchoChannel, "", false, eb);

                    // add the messageid to the cache
                    EchoedCache.Add(e.Message.Id);
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
                    EchoedCache.Add(msg.Id);
                    break;
            }
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
    }
}