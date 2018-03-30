using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Echoer.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Echoer.CommandAttributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class WhiteListedAttribute :CheckBaseAttribute
    {
        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            var config = ctx.Services.GetService<Config>();

            if (!config.WhiteListedUserIds.Contains(ctx.User.Id))
                return Task.FromResult(false);
            return Task.FromResult(true);
        }
    }
}
