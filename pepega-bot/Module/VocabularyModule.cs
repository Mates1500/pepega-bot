using pepega_bot.Services;

namespace pepega_bot.Module
{
    internal class VocabularyModule
    {
        private readonly DatabaseService _dbService;

        public VocabularyModule(DatabaseService dbService, CommandHandlingService chService)
        {
            _dbService = dbService;

            chService.MessageReceived += OnMessageReceived;
        }

        private async void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            var content = e.Message.Content;
            content = content.Replace(",", "");
            content = content.Replace(".", "");

            var words = content.Split(' ');
            foreach (var word in words)
            {
                await _dbService.InsertOrAddCountByOne(word);
            }
        }
    }
}