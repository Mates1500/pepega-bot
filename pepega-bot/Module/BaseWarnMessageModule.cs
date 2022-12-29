using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using pepega_bot.Services;

namespace pepega_bot.Module
{
    internal class BaseWarnMessageModule: IModule
    {
        private readonly Emoji _alreadyReactedEmoji;
        private readonly Emoji _fuckOffEmoji;
        private readonly IConfiguration _config;

        public BaseWarnMessageModule(IConfigurationService configService, Emoji alreadyReactedEmoji)
        {
            _config = configService.Configuration;
            _alreadyReactedEmoji = alreadyReactedEmoji;
            _fuckOffEmoji = new Emoji(_config["Emojis:FuckOff"]);
        }

        public async Task WarnMessage(Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, SocketReaction react, string warnMessage)
        {
            var downloadedMessage = await message.GetOrDownloadAsync();
            var downloadedChannel = await channel.GetOrDownloadAsync();

            try
            {
                await SendWarnMessage(downloadedMessage, downloadedChannel, react, warnMessage);
            }
            catch (SpammerDetectedException)
            {
                await PolitelyDeny(downloadedMessage);
            }
        }

        private async Task SendWarnMessage(IUserMessage message, IMessageChannel channel, SocketReaction react, string warnMessage)
        {
            var spammerIds = _config.GetSection("SpammerIds").Get<ulong[]>();
            if (spammerIds.Contains(react.UserId))
                throw new SpammerDetectedException();
            if (!(message is IUserMessage userMessage)) return;
            if ((DateTime.UtcNow - message.CreatedAt.UtcDateTime).Days >= 1) return;
            if (!(channel is IGuildChannel)) return;
            if (userMessage.Author.IsBot) return;
            if (react.User.IsSpecified && react.User.Value.IsBot) return;
            if (AlreadyReacted(userMessage)) return;

            await channel.SendMessageAsync(warnMessage);
            await message.AddReactionAsync(_alreadyReactedEmoji);
        }

        private async Task PolitelyDeny(IUserMessage downloadedMessage)
        {
            await downloadedMessage.AddReactionAsync(_fuckOffEmoji);
        }

        private bool AlreadyReacted(IUserMessage message)
        {
            var allAlreadyReacts = message.Reactions.Where(x => x.Key.Equals(_alreadyReactedEmoji));
            return allAlreadyReacts.Any(react => react.Value.IsMe);
        }

        private class SpammerDetectedException : Exception
        {
        }
    }
}