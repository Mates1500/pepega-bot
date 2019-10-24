using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using pepega_bot.Services;

namespace pepega_bot.Module
{
    public class HamagenModule : IHamagenModule
    {
        private readonly IConfiguration _config;

        private readonly Emoji _whiteCheckmark;
        

        public HamagenModule(IConfigurationService configService)
        {
            _config = configService.Configuration;

            _whiteCheckmark = new Emoji(_config["Emojis:WhiteCheckmark"]);
        }

        public async Task HandleHamagenEmoteReact(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction react)
        {
            var downloadedMessage = await message.GetOrDownloadAsync();
            if (!(downloadedMessage is IUserMessage userMessage)) return;
            if (userMessage.Author.IsBot) return;
            if (react.User.IsSpecified && react.User.Value.IsBot) return;
            if (AlreadyReacted(userMessage)) return;
            var hamagenUserId = _config["UserIds:Hamagen"];
            var calleeId = react.UserId;
            await channel.SendMessageAsync($"Mayor <@{hamagenUserId}>, one of your town villagers, <@{calleeId}>, requests your help!");
            await downloadedMessage.AddReactionAsync(_whiteCheckmark);
        }

        private bool AlreadyReacted(IUserMessage message)
        {
            var allCheckMarkReacts = message.Reactions.Where(x => x.Key.Equals(_whiteCheckmark));
            return allCheckMarkReacts.Any(react => react.Value.IsMe);
        }
    }
}