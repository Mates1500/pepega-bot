using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using Discord;
using Microsoft.Extensions.Configuration;
using pepega_bot.Services;
using Quartz;

namespace pepega_bot.Module
{
    internal class CustomMessageContainer
    {
        public IMessage Message;
        public DateTime TimeAcquired;
        public bool AlreadyPosted { get; set; }

        public CustomMessageContainer(IMessage message, DateTime timeAcquired)
        {
            Message = message;
            TimeAcquired = timeAcquired;
            AlreadyPosted = false;
        }

        public void UpdateMessage(IMessage message)
        {
            Message = message;
            AlreadyPosted = false;
        }
    }
    internal class TobikExposerModule: IModule
    {
        private readonly IConfiguration _config;
        private readonly List<ulong> _allowedChannels;
        private readonly ulong _tobikId;
        private readonly IScheduler _scheduler;
        private readonly IServiceContainer _jobContainer;

        private readonly Emote _teletobiesEmote;

        private readonly Dictionary<ulong, CustomMessageContainer> _cachedMessages;
        private readonly ulong[] _allowedAdminIds;

        public TobikExposerModule(IConfigurationService config, CommandHandlingService chService, IScheduler scheduler,
            IServiceContainer jobContainer)
        {
            _config = config.Configuration;
            _scheduler = scheduler;
            _jobContainer = jobContainer;

            _cachedMessages = new Dictionary<ulong, CustomMessageContainer>();

            _allowedChannels = _config.GetSection("TobikExposure:Channels").Get<ulong[]>().ToList();
            _allowedAdminIds = _config.GetSection("TobikExposure:ApprovedAdminIds").Get<ulong[]>();
            _tobikId = ulong.Parse(_config["UserIds:Tobik"]);
            _teletobiesEmote = Emote.Parse(_config["Emotes:Teletobies"]);

            chService.MessageReceived += OnMessage;
            chService.MessageUpdated += OnMessageUpdated;
            chService.MessageRemoved += OnMessageRemoved;

            AddJobsToContainer();
            ScheduleJobs();
        }

        private void AddJobsToContainer()
        {
            var dailyJob = new DailyCleanJob(this);

            _jobContainer.AddService(typeof(DailyCleanJob), dailyJob);
        }

        private async void ScheduleJobs()
        {
            var dailyJob = JobBuilder.Create<DailyCleanJob>()
                .WithIdentity("dailyCleanJob", "dailyCleanGroup")
                .Build();

            var dailyTriggerVals = _config["TobikExposure:Schedule:DailyClean"].Split(':')
                .Select(x => Convert.ToUInt16(x)).ToList();
            var dailyHour = dailyTriggerVals[0];
            var dailyMinute = dailyTriggerVals[1];
            var dailyTrigger = TriggerBuilder.Create()
                .WithIdentity("dailyCleanTrigger", "dailyCleanGroup")
                .StartNow()
                .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(dailyHour, dailyMinute))
                .Build();

            await _scheduler.ScheduleJob(dailyJob, dailyTrigger);
        }

        public void Clean()
        {
            var messagesOlderThanWeek =
                _cachedMessages.Where(x => (DateTime.Now - x.Value.TimeAcquired) > TimeSpan.FromDays(7));

            foreach (var message in messagesOlderThanWeek)
            {
                _cachedMessages.Remove(message.Key);
            }
        }

        private void MentalCheckup(MessageReceivedEventArgs e)
        {
            var message = new StringBuilder();
            message.Append($"Mental Checkup for <@{_tobikId}>" + Environment.NewLine);
            message.Append("Diagnosis: RETARDED" + Environment.NewLine);
            message.Append("Recommended treatment: Piškot overdose");
            e.Message.Channel.SendMessageAsync(message.ToString());
        }

        private void OnMessage(object sender, MessageReceivedEventArgs e)
        {
            if (!_allowedChannels.Contains(e.Message.Channel.Id))
                return;
            if (_allowedAdminIds.Contains(e.Message.Author.Id) && e.Message.Content.ToUpper() == "!MENTALCHECKUP")
            {
                MentalCheckup(e);
                return;
            }
            if (e.Message.Author.Id != _tobikId)
                return;

            if (!_cachedMessages.ContainsKey(e.Message.Id))
            {
                var message = new CustomMessageContainer(e.Message, DateTime.Now);
                _cachedMessages.Add(e.Message.Id, message);
            }
        }


        private void OnMessageRemoved(object sender, MessageRemovedEventArgs e)
        {
            if (!_allowedChannels.Contains(e.Channel.Id))
                return;

            var success = _cachedMessages.TryGetValue(e.Message.Id, out CustomMessageContainer previousMessage);

            if (success && !previousMessage.AlreadyPosted)
            {
                if (previousMessage.Message.Author.IsBot)
                    return;

                previousMessage.Message.Channel.SendMessageAsync(_teletobiesEmote + " DELETE " + _teletobiesEmote +
                                                                 Environment.NewLine + previousMessage.Message.Content);
                previousMessage.AlreadyPosted = true;
            }
        }

        private void OnMessageUpdated(object sender, MessageUpdatedEventArgs e)
        {
            if (!_allowedChannels.Contains(e.Channel.Id))
                return;

            var success =_cachedMessages.TryGetValue(e.Message.Id, out CustomMessageContainer previousMessage);

            if (success && !previousMessage.AlreadyPosted)
            {
                if (previousMessage.Message.Author.IsBot)
                    return;

                if (previousMessage.Message.Content == e.NewMessage.Content || e.NewMessage.Content is null)
                    return;

                previousMessage.Message.Channel.SendMessageAsync(_teletobiesEmote + " EDIT " + _teletobiesEmote +
                                                                 Environment.NewLine + previousMessage.Message.Content);
                previousMessage.AlreadyPosted = true;

                previousMessage.UpdateMessage(e.NewMessage);
            }
        }
    }
}