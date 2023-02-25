using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using pepega_bot.Database.RingFit;
using pepega_bot.Services;
using Quartz;

namespace pepega_bot.Module
{
    internal class RingFitConstants
    {
        public const string DailyMsgIdentifier = "rf_daily";
        public const string ButtonWithValueClickIdentifier = "button_value";
        public const string ButtonCustomClickIdentifier = "button_custom";
        public const string ButtonRemove = "button_remove";
        public const string CustomValueIdentifier = "custom_value";
        public const string DateTimeFormat = "dd/MM/yyyy";
    }

    internal class RingFitModule: IModule
    {
        private readonly DatabaseService _dbService;
        private readonly IConfiguration _config;
        private readonly IScheduler _scheduler;
        private readonly IServiceContainer _jobContainer;

        private readonly Dictionary<string, int> _minuteScoresMap;
        private readonly ulong[] _allowedAuthorIds;
        private readonly ulong _ringFitChannelId;
        private readonly ISocketMessageChannel _ringFitChannel;

        private readonly Emoji _pencilEmoji;
        private readonly Emoji _trashEmoji;

        private const string DailyMessageHeader = "RING FIT DAILY CHALLENGE";

        public RingFitModule(IConfigurationService config, CommandHandlingService chService,
            DiscordSocketClient dsc, IScheduler scheduler, IServiceContainer jobContainer, DatabaseService dbService)
        {
            _dbService = dbService;
            _config = config.Configuration;
            _scheduler = scheduler;
            _jobContainer = jobContainer;

            _allowedAuthorIds = _config.GetSection("RingFit:ApprovedAuthorIds").Get<ulong[]>();
            _ringFitChannelId = Convert.ToUInt64(_config["RingFit:ChannelId"]);
            _ringFitChannel = dsc.GetGuild(Convert.ToUInt64(_config["RingFit:GuildId"]))
                .GetTextChannel(_ringFitChannelId);

            _minuteScoresMap = MapMinuteScores();

            _pencilEmoji = new Emoji(_config["Emojis:Pencil"]);
            _trashEmoji = new Emoji(_config["Emojis:Trash"]);

            chService.MessageReceived += OnMessageReceived;
            chService.ModalSubmitted += OnModalSubmitted;

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

        private bool IsValidReactee(SocketReaction r)
        {
            if (r.User.IsSpecified) // this bot itself should be always cached
                if (r.User.Value.IsBot)
                    return false;
            return true;
        }


        private bool IsApprovedAuthorMessage(IMessageChannel channel, IUserMessage message)
        {
            if (channel.Id != _ringFitChannelId)
                return false;

            if (message.Author.IsBot)
                return false;

            return _allowedAuthorIds.Contains(message.Author.Id);
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

        private MessageComponent BuildInteractButtonsForDate(DateTime dt)
        {
            var dateStr = dt.ToString(RingFitConstants.DateTimeFormat, CultureInfo.InvariantCulture);

            var btnBuilder = new ButtonBuilder();

            // pre-made approximate minute buttons
            var buttonsRow1 = new ActionRowBuilder();
            foreach (var mapping in _minuteScoresMap)
            {
                var emote = Emote.Parse(mapping.Key);

                buttonsRow1.AddComponent(btnBuilder
                    .WithStyle(ButtonStyle.Primary)
                    .WithEmote(emote)
                    .WithLabel($"{mapping.Value}+ min")
                    .WithCustomId($"{RingFitConstants.DailyMsgIdentifier}:{RingFitConstants.ButtonWithValueClickIdentifier},{dateStr},{mapping.Value}")
                    .Build());
            }

            // custom input modal
            var buttonsRow2 = new ActionRowBuilder();
            buttonsRow2.AddComponent(btnBuilder
                .WithStyle(ButtonStyle.Success)
                .WithEmote(_pencilEmoji)
                .WithLabel("Jiné")
                .WithCustomId($"{RingFitConstants.DailyMsgIdentifier}:{RingFitConstants.ButtonCustomClickIdentifier},{dateStr}")
                .Build());

            // remove today's result
            buttonsRow2.AddComponent(btnBuilder
                .WithStyle(ButtonStyle.Danger)
                .WithEmote(_trashEmoji)
                .WithLabel("Smazat")
                .WithCustomId($"{RingFitConstants.DailyMsgIdentifier}:{RingFitConstants.ButtonRemove},{dateStr}")
                .Build());

            return new ComponentBuilder()
                .AddRow(buttonsRow1)
                .AddRow(buttonsRow2)
                .Build();
        }

        public async Task<string> GetContentsForDailyMessage(DateTime dt)
        {
            var dateStr = dt.ToString(RingFitConstants.DateTimeFormat, CultureInfo.InvariantCulture);
            var msgSb = new StringBuilder();
            msgSb.AppendLine($"{DailyMessageHeader} {dateStr}");
            msgSb.AppendLine("Zaklikněte, jak dlouho jste cvičili v tento den tlačítkem pod zprávou.");

            var reactsForThisDay = await _dbService.GetReactsForDay(dt);

            if (reactsForThisDay.Any())
            {
                msgSb.Append(Environment.NewLine);
                msgSb.AppendLine("Aktuální výsledky:");

                foreach (var react in reactsForThisDay)
                {
                    msgSb.Append($"<@{react.UserId}>: {react.MinuteValue}");
                    if (react.IsApproximateValue)
                        msgSb.Append("+");
                    msgSb.AppendLine(" minut");
                }
            }

            return msgSb.ToString();
        }

        public async Task UpdateDailyMessage(DateTime dt)
        {
            var dailyMessage = _dbService.GetDailyMessageFor(dt);

            if (await _ringFitChannel.GetMessageAsync(dailyMessage.MessageId) is not RestUserMessage discordMessage)
                return;

            await discordMessage.ModifyAsync(async msg => msg.Content = await GetContentsForDailyMessage(dt));
        }

        public async Task PostDailyMessage()
        {
            var todayDate = DateTime.Today;

            var contentsStr = await GetContentsForDailyMessage(todayDate);

            var buttonsComponent = BuildInteractButtonsForDate(todayDate);


            var message = await _ringFitChannel.SendMessageAsync(contentsStr, components: buttonsComponent);
            await _dbService.AddOrUpdateDailyMessage(new RingFitMessage
            {
                MessageId = message.Id,
                MessageTime = message.Timestamp.DateTime,
                MessageType = RingFitMessageType.Daily
            });

        }

        public async void OnModalSubmitted(object sender, SocketModal socketModal)
        {
            if (!socketModal.Data.CustomId.StartsWith(RingFitConstants.DailyMsgIdentifier))
                return;

            var args = socketModal.Data.CustomId[(RingFitConstants.DailyMsgIdentifier.Length + 1)..]
                .Split(",");
            var date = DateTime.ParseExact(args[0], RingFitConstants.DateTimeFormat, CultureInfo.InvariantCulture);
            var userId = ulong.Parse(args[1]);
            var messageId = ulong.Parse(args[2]);

            var components = socketModal.Data.Components.ToList();
            var inputValStr = components.First(x => x.CustomId == RingFitConstants.CustomValueIdentifier).Value;

            if (!uint.TryParse(inputValStr, out uint inputValInt) || inputValInt < 1)
            {
                await socketModal.RespondAsync(
                    $"Pouze kladná celá čísla jsou akceptována, vaše hodnota \"{inputValStr}\" je neplatná.",
                    ephemeral: true);
                return;
            }

            await _dbService.InsertOrUpdateRingFitReact(new RingFitReact
            {
                MinuteValue = inputValInt,
                UserId = userId,
                IsApproximateValue = false,
                MessageTime = date,
                MessageId = messageId
            });

            await socketModal.RespondAsync($"Vaše hodnota \"{inputValInt}\" byla zaznamenána.", ephemeral: true);
            await UpdateDailyMessage(date);
        }

        public async Task PostWeeklyStats()
        {
            var weeklyResults = await _dbService.GetReactsForWeekIn(DateTime.Now);  // TODO: IDateTimeProvider for unit tests?

            var finalSortedResults = weeklyResults
                .GroupBy(x => x.UserId).Select(x => new
                {
                    UserId = x.Key, 
                    MinutesTotal = x.Sum(y => y.MinuteValue),
                    IsApproximate = x.Any(z => z.IsApproximateValue)
                }).OrderByDescending(x => x.MinutesTotal).ToList();

            var sb = new StringBuilder();
            sb.Append("Statistiky účasti za aktuální týden:" + Environment.NewLine);

            for (var i = 0; i < finalSortedResults.Count; i++)
            {
                sb.Append($"{i + 1}. <@{finalSortedResults[i].UserId}> - {finalSortedResults[i].MinutesTotal}");
                if (finalSortedResults[i].IsApproximate)
                    sb.Append("+");
                sb.AppendLine(" minut");
            }

            sb.Append(Environment.NewLine + "Gratulujeme všem účastníkům!");

            await _ringFitChannel.SendMessageAsync(sb.ToString());
        }

    }
}