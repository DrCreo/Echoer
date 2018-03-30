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

namespace Echoer.Commands
{
    public class AdminCommands : BaseCommandModule
    {

        [Command("Update"), WhiteListed, Description("Updates the bot.")]
        public async Task Update(CommandContext ctx)
        {
            var config = ctx.Services.GetService<Config>();

            if (config.UpdateBatPath == "")
                return;
            System.Diagnostics.Process.Start(config.UpdateBatPath);
            return;
        }

        [Command("uploadlog"), WhiteListed, Description("uploads the log.")]
        public async Task UploadLog(CommandContext ctx)
        {
            await ctx.RespondWithFileAsync("log.txt");
        }
    }
}
