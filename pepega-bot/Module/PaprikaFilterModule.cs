﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using pepega_bot.Services;
using pepega_bot.Utils;

namespace pepega_bot.Module
{
    internal class PaprikaFilterModule: IModule
    {
        private readonly IDiscordClient _discordClient;
        private readonly IConfiguration _config;
        private readonly Emote _linkRageEmote;
        public PaprikaFilterModule(IConfigurationService configService, CommandHandlingService chService, IDiscordClient discordClient)
        {
            _discordClient = discordClient;
            _config = configService.Configuration;

            chService.MessageReceived += MessageReceivedAsync;
            chService.MessageUpdated += MessageUpdatedAsync;

            _linkRageEmote = Emote.Parse(_config["Emotes:GooseHonkKnife"]);
        }

        private async void MessageReceivedAsync(object sender, MessageReceivedEventArgs e)
        {
            if (e.Message.Author.Id == ulong.Parse(_config["UserIds:Paprika"]))
            {
                await HandlePaprikaMessage(e.Message);
            }
        }

        private async void MessageUpdatedAsync(object sender, MessageUpdatedEventArgs e)
        {
            if (e.NewMessage.Author.Id == ulong.Parse(_config["UserIds:Paprika"]))
            {
                await HandlePaprikaMessage(e.NewMessage);
            }
        }

        public async Task HandlePaprikaMessage(SocketMessage message)
        {
            var repairedMessage = RepairMessage(message.Content);

            if (!(message is IUserMessage)) return;

            if (repairedMessage.Phrases.Count < 1)
            {
                await ((IUserMessage) message).RemoveReactionAsync(_linkRageEmote, _discordClient.CurrentUser);
                return;
            }

            var sb = new StringBuilder();
            var paprikaId = ulong.Parse(_config["UserIds:Paprika"]);
            sb.Append($"Uh oh, resident <@{paprikaId}>, I'm afraid there have been some errors in your message according to your ž-speak!");
            foreach (var phrase in repairedMessage.Phrases)
            {
                sb.Append(Environment.NewLine + $"{phrase.OriginalPhrase} -> {phrase.CorrectedPhrase}");
            }

            await ((IUserMessage)message).AddReactionAsyncFixed(_linkRageEmote);
        }

        private RepairedMessage RepairMessage(string content)
        {
            var words = content.ToLower().Split(" ");
            var repairedMessage = new RepairedMessage();
            foreach (var word in words)
            {
                if (word.ToLower().StartsWith("https://")) continue;
                if (word.ToLower().StartsWith("http://")) continue;
                if (word.StartsWith("<:")) continue;
                if (!word.Contains("g")) continue;

                var repairedWord = word.Replace("g", "ž");
                repairedMessage.Phrases.Add(new PhraseCorrection(word, repairedWord));
            }

            return repairedMessage;
        }

    }

    internal class RepairedMessage
    {
        public IList<PhraseCorrection> Phrases { get; }

        public RepairedMessage()
        {
            Phrases = new List<PhraseCorrection>();
        }
    }

    internal class PhraseCorrection
    {
        public string OriginalPhrase { get; }

        public string CorrectedPhrase { get; }

        public PhraseCorrection(string originalPhrase, string correctedPhrase)
        {
            OriginalPhrase = originalPhrase;
            CorrectedPhrase = correctedPhrase;
        }
    }
}