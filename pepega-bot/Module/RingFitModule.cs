using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using pepega_bot.Services;
using Quartz;

namespace pepega_bot.Module
{
    internal class RingFitModule: IModule
    {
        private readonly DatabaseService _dbService;
        private readonly IConfiguration _config;
        private readonly IScheduler _scheduler;
        private readonly IServiceContainer _jobContainer;

        private readonly Dictionary<string, int> _minuteScoresMap;
        private readonly List<string> _trackedReacts;
        private readonly ulong[] _allowedAuthorIds;
        private readonly ulong _ringFitChannelId;
        private readonly ISocketMessageChannel _ringFitChannel;

        private const string DailyMessageHeader = "RING FIT DAILY CHALLENGE";
        private readonly string _dailyMessageBase;

        public RingFitModule(IConfigurationService config, CommandHandlingService chService,
            DiscordSocketClient dsc, IScheduler scheduler, IServiceContainer jobContainer)
        {
            _dbService = new DatabaseService(config);
            _config = config.Configuration;
            _scheduler = scheduler;
            _jobContainer = jobContainer;


            _allowedAuthorIds = _config.GetSection("RingFit:ApprovedAuthorIds").Get<ulong[]>();
            _ringFitChannelId = Convert.ToUInt64(_config["RingFit:ChannelId"]);
            _ringFitChannel = dsc.GetGuild(Convert.ToUInt64(_config["RingFit:GuildId"]))
                .GetTextChannel(_ringFitChannelId);

            _minuteScoresMap = MapMinuteScores();
            _trackedReacts = _minuteScoresMap.Keys.ToList();

            _dailyMessageBase = ConstructDailyMessageBase();

            chService.ReactAdded += OnReactAdded;
            chService.ReactRemoved += OnReactRemoved;
            chService.MessageRemoved += OnMessageRemoved;
            chService.MessageReceived += OnMessageReceived;

            AddJobsToContainer();
            ScheduleJobs();
        }

        private void AddJobsToContainer()
        {
            var dailyJob = new DailyPostJob(this);
            var weeklyJob = new WeeklySummaryJob(this);

            _jobContainer.AddService(typeof(DailyPostJob), dailyJob);
            _jobContainer.AddService(typeof(WeeklySummaryJob), weeklyJob);
        }

        private async void ScheduleJobs()
        {
            var weeklyJob = JobBuilder.Create<WeeklySummaryJob>()
                .WithIdentity("weeklyJob", "weeklyGroup")
                .Build();

            // TODO: Clean this shit up, function is getting too long
            var weeklyTriggerVals = _config["RingFit:Schedule:SundayWeeklyStatsList"].Split(':')
                .Select(x => Convert.ToUInt16(x)).ToList();
            var weeklyHour = weeklyTriggerVals[0];
            var weeklyMinute = weeklyTriggerVals[1];

            var weeklyTrigger = TriggerBuilder.Create()
                .WithIdentity("weeklyTrigger", "weeklyGroup")
                .StartNow()
                .WithSchedule(CronScheduleBuilder.WeeklyOnDayAndHourAndMinute(DayOfWeek.Sunday, weeklyHour, weeklyMinute))
                .Build();

            var dailyJob = JobBuilder.Create<DailyPostJob>()
                .WithIdentity("dailyJob", "dailyGroup")
                .Build();

            var dailyTriggerVals = _config["RingFit:Schedule:DailyChallengePost"].Split(':')
                .Select(x => Convert.ToUInt16(x)).ToList();
            var dailyHour = dailyTriggerVals[0];
            var dailyMinute = dailyTriggerVals[1];

            var dailyTrigger = TriggerBuilder.Create()
                .WithIdentity("dailyTrigger", "dailyGroup")
                .StartNow()
                .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(dailyHour, dailyMinute))
                .Build();

            await _scheduler.ScheduleJob(weeklyJob, weeklyTrigger);
            await _scheduler.ScheduleJob(dailyJob, dailyTrigger);
        }

        private Dictionary<string, int> MapMinuteScores()
        {
            // mapping expected in form ["EmoteKey", MinuteValue] per item, I couldn't do dict, because colons fucked up hierarchy in key in .NET Core IConfig
            var dict = new Dictionary<string, int>();
            foreach (var pair in _config.GetSection("RingFit:ScoreMapping").GetChildren())
            {
                var currentPair = pair.Get<string[]>();

                if (currentPair.Length != 2)
                    throw new FormatException("Score mapping per item is expected in form [\"EmoteKey\", MinuteValue]");

                var emoteKey = currentPair[0];
                var minuteValue = Convert.ToInt16(currentPair[1]);

                dict.Add(emoteKey, minuteValue);
            }
                
            return dict;
        }

        private string ConstructDailyMessageBase()
        {
            var sb = new StringBuilder();
            sb.Append("Cvičili jste dnes: " + Environment.NewLine);
            var keysSortedByVal = _minuteScoresMap.OrderBy(x => x.Value).Select(x => x.Key).ToList();
            for (var i = 0; i < keysSortedByVal.Count; i++)
            {
                var isLastIteration = i == keysSortedByVal.Count - 1;
                sb.Append(_minuteScoresMap[keysSortedByVal[i]]); // min number of mins
                if (!isLastIteration)
                {
                    sb.Append("-");
                    sb.Append(_minuteScoresMap[keysSortedByVal[i + 1]]); // max number of mins = min number of mins of next sorted val
                }
                else
                {
                    sb.Append("+");
                }
                sb.Append(" min ");
                sb.Append(keysSortedByVal[i]); // the actual emote
                if (!isLastIteration)
                {
                    sb.Append(Environment.NewLine);
                }
            }

            sb.Append("?");
            return sb.ToString();
        }

        private bool IsValidReactee(SocketReaction r)
        {
            if (r.User.IsSpecified) // this bot itself should be always cached
                if (r.User.Value.IsBot)
                    return false;
            return true;
        }

        private bool IsDailyBotMessage(ISocketMessageChannel channel, IUserMessage message)
        {
            if (channel.Id != _ringFitChannelId)
                return false;

            if (!message.Author.IsBot)
                return false;

            return message.Content.StartsWith(DailyMessageHeader);
        }

        private bool IsApprovedAuthorMessage(ISocketMessageChannel channel, IUserMessage message)
        {
            if (channel.Id != _ringFitChannelId)
                return false;

            if (message.Author.IsBot)
                return false;

            return _allowedAuthorIds.Contains(message.Author.Id);
        }

        private string EmoteToStringCode(Emote e)
        {
            return e.ToString();
        }

        private async void OnReactAdded(object sender, ReactionAddedEventArgs e)
        {
            if (!IsValidReactee(e.React))
                return;

            if (!IsDailyBotMessage(e.Channel, await e.Message.GetOrDownloadAsync()))
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
            if (!IsDailyBotMessage(e.Channel, await e.Message.GetOrDownloadAsync()))
                return;

            await _dbService.RemoveRingFitReact(e.React.UserId, e.React.Emote.ToString(), e.React.MessageId);
        }

        private async void OnMessageRemoved(object sender, MessageRemovedEventArgs e)
        {
            return; // TODO: resolve - can't download a removed message if not cached previously...

            if (!IsApprovedAuthorMessage(e.Channel, await e.Message.GetOrDownloadAsync() as IUserMessage))
                return;

            var message = await e.Message.GetOrDownloadAsync();

            await _dbService.RemoveRingFitReactsFor(message.Id);
        }

        private async void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            if (!IsApprovedAuthorMessage(e.Message.Channel, e.Message as IUserMessage))
                return;

            switch (e.Message.Content.ToUpper())
            {
                case "!WEEKLYSTATS":
                    await PostWeeklyStats();
                    break;
                case "!DAILYMESSAGE":
                    await PostDailyMessage();
                    break;
            }
        }

        public async Task PostDailyMessage()
        {
            var msgSb = new StringBuilder();
            msgSb.Append($"{DailyMessageHeader} " + DateTime.Now.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) +
                         Environment.NewLine);
            msgSb.Append(_dailyMessageBase);

            var result = msgSb.ToString();
            
            var sentMessage = await _ringFitChannel.SendMessageAsync(result);
            foreach (var emote in MapMinuteScores().Select(e => Emote.Parse(e.Key)))
            {
                await sentMessage.AddReactionAsync(emote);
            }
        }

        public async Task PostWeeklyStats()
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

            var sortedResults = summedResults.OrderByDescending(x => x.Value).ToList();


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