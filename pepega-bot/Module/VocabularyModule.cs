using System;
using Microsoft.Extensions.Configuration;
using pepega_bot.Services;

namespace pepega_bot.Module
{
    internal class VocabularyModule
    {
        private readonly DatabaseService _dbService;
        private readonly IConfiguration _config;

        public VocabularyModule(DatabaseService dbService, IConfigurationService configService, CommandHandlingService chService)
        {
            _dbService = dbService;
            _config = configService.Configuration;

            chService.MessageReceived += OnMessageReceived;
        }

        private async void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            if (e.Message.Author.IsBot) return;

            var content = e.Message.Content;
            var trimCharSequence = _config["TrimVocabularyCharacters"];
            foreach (var c in trimCharSequence)
            {
                content = content.Replace(c, char.MinValue);
            }

            var words = content.Split(' ');
            foreach (var word in words)
            {
                var normalizedWord = word.ToLower().Trim();
                await _dbService.InsertOrAddCountByOne(normalizedWord);
            }
        }
    }
}