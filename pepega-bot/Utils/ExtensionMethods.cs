using System.Threading.Tasks;
using Discord;
using Discord.Net;

namespace pepega_bot.Utils;

internal static class ExtensionMethods
{
    public static async Task AddReactionAsyncFixed(this IUserMessage message, IEmote emote, RequestOptions options = null)
    {
        try
        {
            await message.AddReactionAsync(emote, options);
        }
        catch (HttpException ex)
        {
            if (!ex.Message.ToLower().Contains("reaction blocked"))
                throw;
        }
    }
}