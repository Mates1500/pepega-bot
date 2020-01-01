using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace pepega_bot.Utils
{
    public class ReactInfo
    {
        public ulong ReactAuthorId { get; }
        public ulong GuildId { get; }
        public string MessageLink { get; }


        public ReactInfo(IConfiguration config, Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction react)
        {
            ReactAuthorId = react.UserId;
            GuildId = ((IGuildChannel)channel).GuildId;
            MessageLink = $"{config["DiscordBaseUrl"]}/channels/{GuildId}/{channel.Id}/{message.Id}";
        }
    }
}