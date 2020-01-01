using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace pepega_bot.Module
{
    public class BaseWarnMessageModule
    {
        private readonly Emoji _alreadyReactedEmoji;

        public BaseWarnMessageModule(Emoji alreadyReactedEmoji)
        {
            _alreadyReactedEmoji = alreadyReactedEmoji;
        }

        public async Task WarnMessage(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction react, string warnMessage)
        {
            var downloadedMessage = await message.GetOrDownloadAsync();
            if (!(downloadedMessage is IUserMessage userMessage)) return;
            if ((DateTime.UtcNow - downloadedMessage.CreatedAt.UtcDateTime).Days >= 1) return;
            if (!(channel is IGuildChannel)) return;
            if (userMessage.Author.IsBot) return;
            if (react.User.IsSpecified && react.User.Value.IsBot) return;
            if (AlreadyReacted(userMessage)) return;

            await channel.SendMessageAsync(warnMessage);
            await downloadedMessage.AddReactionAsync(_alreadyReactedEmoji);
        }

        private bool AlreadyReacted(IUserMessage message)
        {
            var allAlreadyReacts = message.Reactions.Where(x => x.Key.Equals(_alreadyReactedEmoji));
            return allAlreadyReacts.Any(react => react.Value.IsMe);
        }
    }
}