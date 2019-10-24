using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using pepega_bot.Services;

namespace pepega_bot.Module
{
    public class PaprikaFilterModule : IPaprikaFilterModule
    {
        private readonly IConfiguration _config;

        public PaprikaFilterModule(IConfigurationService configService)
        {
            _config = configService.Configuration;
        }

        public async Task HandlePaprikaMessage(SocketMessage message)
        {
            var repairedMessage = RepairMessage(message.Content);

            if (repairedMessage.Phrases.Count < 1) return;

            var sb = new StringBuilder();
            var paprikaId = ulong.Parse(_config["UserIds:Paprika"]);
            sb.Append($"Uh oh, resident <@{paprikaId}>, I'm afraid there have been some errors in your message according to your ž-speak!");
            foreach (var phrase in repairedMessage.Phrases)
            {
                sb.Append(Environment.NewLine + $"{phrase.OriginalPhrase} -> {phrase.CorrectedPhrase}");
            }

            await message.Channel.SendMessageAsync(sb.ToString());

        }

        private RepairedMessage RepairMessage(string content)
        {
            var words = content.Split(" ");
            var repairedMessage = new RepairedMessage();
            foreach (var word in words)
            {
                if (word.ToLower().StartsWith("https://")) continue;
                if (word.ToLower().StartsWith("https://")) continue;
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
        public string OriginalPhrase { get; set; }

        public string CorrectedPhrase { get; set; }

        public PhraseCorrection(string originalPhrase, string correctedPhrase)
        {
            OriginalPhrase = originalPhrase;
            CorrectedPhrase = correctedPhrase;
        }
    }
}