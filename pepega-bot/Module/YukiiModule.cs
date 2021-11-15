using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using pepega_bot.Database;
using pepega_bot.Services;
using Quartz;

namespace pepega_bot.Module
{
    internal class YukiiModule: IModule
    {
        private readonly DatabaseService _dbService;
        private readonly IScheduler _scheduler;
        private readonly IServiceContainer _jobContainer;
        private readonly IConfiguration _config;
        private readonly ulong _yukiiUserId;

        private readonly Regex _emoteRegex;
        private readonly ISocketMessageChannel _kanalChannel;
        private readonly ulong[] _allowedAdminIds;
        private readonly Dictionary<int, Emoji> _countToEmojiMappings;

        public YukiiModule(DatabaseService dbService, IConfigurationService configService,
            CommandHandlingService chService, DiscordSocketClient dsc, IScheduler scheduler,
            IServiceContainer jobContainer)
        {
            _dbService = dbService;
            _scheduler = scheduler;
            _jobContainer = jobContainer;
            _config = configService.Configuration;
            _yukiiUserId = ulong.Parse(_config["UserIds:Yukii"]);
            _kanalChannel = dsc.GetGuild(ulong.Parse(_config["Yukii:GuildId"]))
                .GetTextChannel(ulong.Parse(_config["Yukii:SummaryChannelId"]));
            _allowedAdminIds = _config.GetSection("Yukii:ApprovedAdminIds").Get<ulong[]>();

            _countToEmojiMappings = new Dictionary<int, Emoji>();
            MapCountsToEmotes();

            _emoteRegex = new Regex(
                "(\\u00a9|\\u00ae|[\\u2000-\\u3300]|\\ud83c[\\ud000-\\udfff]|\\ud83d[\\ud000-\\udfff]|\\ud83e[\\ud000-\\udfff]|<:.+?:\\d+>)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.ECMAScript);

            chService.MessageReceived += MessageReceivedAsync;

            AddJobsToContainer();
            ScheduleJobs();
        }

        private void AddJobsToContainer()
        {
            var weeklyJob = new WeeklyStatisticsJob(this);

            _jobContainer.AddService(typeof(WeeklyStatisticsJob), weeklyJob);
        }

        private async void ScheduleJobs()
        {
            var weeklyJob = JobBuilder.Create<WeeklyStatisticsJob>()
                .WithIdentity("weeklyYukiiStatsJob", "weeklyYukiiStatsGroup")
                .Build();

            var weeklyTriggerVals = _config["Yukii:Schedule:SundayWeeklyStatsList"].Split(':')
                .Select(x => Convert.ToUInt16(x)).ToList();
            var weeklyHour = weeklyTriggerVals[0];
            var weeklyMinute = weeklyTriggerVals[1];

            var weeklyTrigger = TriggerBuilder.Create()
                .WithIdentity("weeklyYukiiStatsTrigger", "weeklyYukiiStatsGroup")
                .StartNow()
                .WithSchedule(CronScheduleBuilder.WeeklyOnDayAndHourAndMinute(DayOfWeek.Sunday, weeklyHour, weeklyMinute))
                .Build();

            await _scheduler.ScheduleJob(weeklyJob, weeklyTrigger);
        }

        private void MapCountsToEmotes()
        {
            var origDict = _config.GetSection("Yukii:EmoteCountMappings").Get<Dictionary<string, string>>();

            foreach (var kv in origDict)
            {
                _countToEmojiMappings.Add(int.Parse(kv.Key), new Emoji(kv.Value));
            }
        }

        private async Task EvaluateYukiiMessage(SocketMessage message)
        {
            var matches = _emoteRegex.Matches(message.Content);

            var messageLength = 0;
            var lastMatchFinalIndex = 0;

            foreach (Match match in matches)
            {
                var g = match.Groups[0];
                messageLength += g.Index - lastMatchFinalIndex;
                lastMatchFinalIndex = g.Index + g.Length;
            }

            messageLength += message.Content.Length - lastMatchFinalIndex;

            var emoteStatMatch = new EmoteStatMatch
            {
                UserId = message.Author.Id,
                MessageLength = messageLength,
                MatchesCount = matches.Count,
                TimestampUtc = message.Timestamp.DateTime,
                MessageId = message.Id
            };

            await _dbService.InsertEmoteStatMatch(emoteStatMatch);

            if (_countToEmojiMappings.ContainsKey(matches.Count))
            {
                await message.AddReactionAsync(_countToEmojiMappings[matches.Count]);
            }
        }

        private bool IsApprovedAdminMessage(ISocketMessageChannel channel, IUserMessage message)
        {
            if (channel.Id != _kanalChannel.Id)
                return false;

            if (message.Author.IsBot)
                return false;

            return _allowedAdminIds.Contains(message.Author.Id);
        }

        private async void MessageReceivedAsync(object? sender, MessageReceivedEventArgs e)
        {
            if (e.Message.Author.Id == _yukiiUserId)
                await EvaluateYukiiMessage(e.Message);

            if (!IsApprovedAdminMessage(e.Message.Channel, e.Message as IUserMessage))
                return;

            switch (e.Message.Content.ToUpper())
            {
                case "!YUKIISTATS":
                    await PostWeeklyStats();
                    break;
            }
        }


        public async Task PostWeeklyStats()
        {
            var statMatches = _dbService.GetEmoteStatMatchesForUserAndWeekIn(_yukiiUserId, DateTime.Now).ToList();

            if (statMatches.Count < 1)
                return;

            var sb = new StringBuilder();
            sb.Append($"Weekly Emoji stats for <@{_yukiiUserId}>" + Environment.NewLine);
            sb.Append($"Messages posted: {statMatches.Count}" + Environment.NewLine);
            sb.Append($"Emojis posted: {statMatches.Sum(x => x.MatchesCount)}" + Environment.NewLine);
            sb.Append($"Characters posted (excluding emojis): {statMatches.Sum(x => x.MessageLength)}" +
                      Environment.NewLine);
            sb.Append($"Average characters per emote: {statMatches.Average(x => x.CharactersPerEmote):0.###}" +
                      Environment.NewLine);
            sb.Append($"Average emotes per message: {statMatches.Average(x => x.MatchesCount):0.###}" +
                      Environment.NewLine);

            var message = sb.ToString();

            await _kanalChannel.SendMessageAsync(message);
        }
    }
}