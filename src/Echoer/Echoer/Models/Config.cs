using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using DSharpPlus;
using DSharpPlus.Entities;

namespace Echoer.Models
{
    public class Config
    {
        [JsonProperty("token")]
        public string Token { get; private set; }

        [JsonProperty("echo-channel-id")]
        public ulong EchoChannelID { get; private set; }

        [JsonProperty("art-channel-id")]
        public ulong ArtChannelID { get; private set; }

        [JsonProperty("reaction-emoji-ids")]
        public List<ulong> ReactionEmojiIDs { get; private set; }

        [JsonProperty("needed-perm")]
        public Permissions NeededPerm { get; private set; }

        [JsonProperty("echoed-cache")]
        public int EchoedCache { get; private set; }

        [JsonProperty("embed_color_hex")]
        public string EmbedColor { get; private set; }

        [JsonProperty("status")]
        public string Status { get; private set; }

        [JsonProperty("prefix")]
        public List<string> Prefix { get; private set; }

        [JsonProperty("whitelisted-userids")]
        public List<ulong> WhiteListedUserIds { get; private set; }

        [JsonProperty("batdirectory-path")]
        public string BatDiectory { get; private set; }


        public static Config DefualtConfig
        {
            get
            {
                return new Config
                {
                    Token = "<token>",
                    EchoChannelID = 0000,
                    ArtChannelID = 0000,
                    ReactionEmojiIDs = new List<ulong>(),
                    NeededPerm = Permissions.ManageChannels,
                    EchoedCache = 25,
                    Status = "out for great art!",
                    Prefix = new List<string>(),
                    WhiteListedUserIds = new List<ulong>(),
                    EmbedColor = DiscordColor.Cyan.ToString(),
                    BatDiectory = ""
                };
            }
        }

    }
}
