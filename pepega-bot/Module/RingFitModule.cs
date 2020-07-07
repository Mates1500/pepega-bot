using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using pepega_bot.Services;

namespace pepega_bot.Module
{
    internal class RingFitModule
    {
        private readonly DatabaseService _dbService;
        private readonly IConfiguration _config;

        private readonly string _linkPepeHypeCode;
        private readonly string _marioYayCode;
        private readonly string _sonicDabCode;
        private readonly string _samusCode;
        private readonly string _linkRageCode;

        private readonly Dictionary<string, int> _minuteScoresMap;
        private readonly List<string> _trackedReacts;
        private readonly ulong[] _allowedAuthorIds;
        private readonly ulong _ringFitChannelId;
        private readonly ISocketMessageChannel _ringFitChannel;

        public RingFitModule(DatabaseService dbService, IConfiguration config, CommandHandlingService chService, DiscordSocketClient dsc)
        {
            _dbService = dbService;
            _config = config;

            _linkPepeHypeCode = _config["Emotes:LinkPepeHype"];
            _marioYayCode = _config["Emotes:MarioYay"];
            _sonicDabCode = _config["Emotes:SonicDab"];
            _samusCode = _config["Emotes:Samus"];
            _linkRageCode = _config["Emotes:LinkRage"];

            _allowedAuthorIds = _config.GetSection("RingFit:ApprovedAuthorIds").Get<ulong[]>();
            _ringFitChannelId = Convert.ToUInt64(_config["RingFit:ChannelId"]);
            _ringFitChannel = dsc.GetGuild(Convert.ToUInt64(_config["RingFit:GuildId"]))
                .GetTextChannel(_ringFitChannelId);

            _minuteScoresMap = MapMinuteScores();
            _trackedReacts = _minuteScoresMap.Keys.ToList();

            chService.ReactAdded += OnReactAdded;
            chService.ReactRemoved += OnReactRemoved;
            chService.MessageRemoved += OnMessageRemoved;
            chService.MessageReceived += OnMessageReceived;
        }

        private Dictionary<string, int> MapMinuteScores()
        {
            return new Dictionary<string, int>
            {
                {_linkPepeHypeCode, 5},
                {_marioYayCode, 15},
                {_sonicDabCode, 30},
                {_samusCode, 45},
                {_linkRageCode, 60}
            };
        }

        private bool ShouldBeSkipped(ISocketMessageChannel channel, IUserMessage message)
        {
            if (channel.Id != _ringFitChannelId)
                return true;

            if (message.Author.IsBot)
                return true;

            if (!_allowedAuthorIds.Contains(message.Author.Id))
                return true;

            return false;
        }

        private string EmoteToStringCode(Emote e)
        {
            return e.ToString();
        }

        private async void OnReactAdded(object sender, ReactionAddedEventArgs e)
        {
            if (ShouldBeSkipped(e.Channel, await e.Message.GetOrDownloadAsync()))
                return;

            if (!(e.React.Emote is Emote emote))
                return;

            if (!_trackedReacts.Contains(EmoteToStringCode(emote)))
                return;

            var message = await e.Message.GetOrDownloadAsync();

            var react = new RingFitReact
            {
                EmoteId = EmoteToStringCode(emote),
                MessageId = message.Id,
                UserId = e.React.UserId,
                MessageTime = message.CreatedAt.LocalDateTime
            };

            await _dbService.InsertRingFitReact(react);
        }

        private async void OnReactRemoved(object sender, ReactionRemovedEventArgs e)
        {
            if (ShouldBeSkipped(e.Channel, await e.Message.GetOrDownloadAsync()))
                return;

            await _dbService.RemoveRingFitReact(e.React.UserId, e.React.MessageId);
        }

        private async void OnMessageRemoved(object sender, MessageRemovedEventArgs e)
        {
            return; // TODO: resolve - can't download a removed message if not cached previously...

            if (ShouldBeSkipped(e.Channel, await e.Message.GetOrDownloadAsync() as IUserMessage))
                return;

            var message = await e.Message.GetOrDownloadAsync();

            await _dbService.RemoveRingFitReactsFor(message.Id);
        }

        private async void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            if (ShouldBeSkipped(e.Message.Channel, e.Message as IUserMessage))
                return;

            if (e.Message.Content.ToUpper() == "!WEEKLYSTATS")
                await PostWeeklyStats();
        }

        private async Task PostWeeklyStats()
        {
            var weeklyResults = _dbService.GetReactsForWeekIn(DateTime.Now);  // TODO: IDateTimeProvider for unit tests?

            var groupedResults = weeklyResults.GroupBy(x => x.UserId);
            var summedResults = new Dictionary<ulong, int>();
            foreach (var group in groupedResults)
            {
                var score = 0;
                foreach (var react in group)
                {
                    if (!_minuteScoresMap.ContainsKey(react.EmoteId))
                        continue;

                    score += _minuteScoresMap[react.EmoteId];
                }
                summedResults.Add(group.Key, score);
            }

            var sortedResults = summedResults.OrderBy(x => x.Value).ToList();


            var sb = new StringBuilder();
            sb.Append("Participation stats for the current week:" + Environment.NewLine);

            for (var i = 0; i < sortedResults.Count; i++)
            {
                sb.Append($"{i + 1}. <@{sortedResults[i].Key}> - {sortedResults[i].Value}+ minutes" + Environment.NewLine);
            }

            sb.Append(Environment.NewLine + "Congratulations to all the participants!");

            await _ringFitChannel.SendMessageAsync(sb.ToString());
        }

    }

    public class RingFitReact
    {
        public int Id { get; set; }
        public string EmoteId { get; set; }
        public ulong UserId { get; set; }
        public ulong MessageId { get; set; }
        public DateTime MessageTime { get; set; }
    }
}