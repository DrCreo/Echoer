using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using Echoer.CommandAttributes;
using System.Threading.Tasks;
using Echoer.Models;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Reflection;
using DSharpPlus.Entities;

namespace Echoer.Commands
{
    public class AdminCommands : BaseCommandModule
    {

        [Command("bat"), WhiteListed, Description("Updates the bot.")]
        public async Task Bat(CommandContext ctx, [RemainingText, Description("")] string batName)
        {

            try
            {
                var bat = batName;

                if (!bat.ToLower().EndsWith(".bat"))
                    bat += ".bat";

                var config = ctx.Services.GetService<Config>();

                bat = config.BatDiectory + (config.BatDiectory.EndsWith('\\') ? string.Empty : "\\") + bat;

                if (!File.Exists(bat))
                {
                    await ctx.RespondAsync($"`{bat}`\nDoes not exist.");
                    return;
                }

                await ctx.RespondAsync($"`Running `{batName}`");
                await ctx.Client.UpdateStatusAsync(new DiscordActivity("the Updating game", ActivityType.Playing));
                System.Diagnostics.Process.Start(bat);
            }
            catch (Exception ex)
            {
                new LogWriter(ex.Message);
            }
        }

        [Command("batlist"), WhiteListed]
        public async Task batlist(CommandContext ctx)
        {
            if (!ctx.Channel.IsPrivate)
                return;
            var files = Directory.GetFiles(ctx.Services.GetService<Config>().BatDiectory);

            var fileString = "```Files in directory:\n\n";

            int i = 1;
            foreach (var f in files)
            {
                var x = f.Split("\\");
                var fn = x[x.Length - 1];
                fileString += $"{i}.{fn}\n";
                i++;
            }
            fileString += "```";

            await ctx.RespondAsync(fileString);
        }

        [Command("uploadlog"), WhiteListed, Description("uploads the log.")]
        public async Task UploadLog(CommandContext ctx)
        {
            try
            {
                var m_exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                await ctx.RespondWithFileAsync(m_exePath + "\\log.txt");
            }
            catch (Exception ex)
            {
                new LogWriter("Upload Log Command.\n" + ex.Message);
            }
        }
    }
}
