using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace pepega_bot
{
    class Program
    {
        static void Main(string[] args)
        {
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        private DiscordSocketClient _client;

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient();
            _client.Log += Log;
            _client.ReactionAdded += ReactionAdded;
            _client.MessageReceived += MessageReceived;

            await _client.LoginAsync(TokenType.Bot, GetToken());
            await _client.StartAsync();

            await Task.Delay(-1);

        }

        private Task MessageReceived(SocketMessage arg)
        {
            //throw new NotImplementedException();
            return Task.CompletedTask;
        }

        private async Task ReactionAdded(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction react)
        { 
            Emote.TryParse("<:callhamagen:634667554689908776>", out var hamagenEmote);
            var checkMark = new Emoji("\u2705");
            if (react.Emote.Equals(hamagenEmote))
            {
                var downloadedMessage = await message.DownloadAsync();
                if (!(downloadedMessage is IUserMessage userMessage)) return;
                if (userMessage.Author.IsBot) return;
                if (AlreadyReacted(userMessage)) return;
                var userId = react.User.Value.Id;
                await channel.SendMessageAsync($"Mayor <@{userId}>, there's some trouble in your town that requires your immediate attention!");
                await downloadedMessage.AddReactionAsync(checkMark);
            }
        }

        private bool AlreadyReacted(IUserMessage message)
        {
            var checkMark = new Emoji("\u2705");

            var allCheckMarkReacts = message.Reactions.Where(x => x.Key.Equals(checkMark));
            return allCheckMarkReacts.Any(react => react.Value.IsMe);
        }

        private string GetToken()
        {
            return "NjM2NjEwNjk3MjkxMzY2NDEy.XbCIAQ.FFA8f7XrzaQPbAty5xgxrDzjW6Q"; // TODO: Replace
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
