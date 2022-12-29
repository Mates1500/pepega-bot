using System;
using Discord;
using Microsoft.Extensions.Configuration;
using pepega_bot.Services;
using pepega_bot.Utils;

namespace pepega_bot.Module
{
    internal class HamagenModule: IModule
    {
        private readonly IConfiguration _config;
        private readonly BaseWarnMessageModule _baseWarnMessageModule;
        private readonly string _hamagenUserId;
        private readonly Emote _hamagenEmote;


        public HamagenModule(IConfigurationService configService, CommandHandlingService chService)
        {
            _config = configService.Configuration;
            _hamagenUserId = _config["UserIds:Hamagen"];
            _hamagenEmote = Emote.Parse(_config["Emotes:CallHamagen"]);
            var whiteCheckmark = new Emoji(_config["Emojis:WhiteCheckmark"]);

            _baseWarnMessageModule = new BaseWarnMessageModule(configService, whiteCheckmark);

            chService.ReactAdded += ReactionAddedAsync;
        }

        private async void ReactionAddedAsync(object sender, ReactionAddedEventArgs e)
        {
            if (!e.React.Emote.Equals(_hamagenEmote)) return;

            var reactInfo = new ReactInfo(_config, e.Message, await e.Channel.GetOrDownloadAsync(), e.React);
            var warnMessage =
                $"Mayor <@{_hamagenUserId}>, one of your town villagers, <@{reactInfo.ReactAuthorId}>, requests your help!" +
                Environment.NewLine + $"Villager's letter: {reactInfo.MessageLink}";

            await _baseWarnMessageModule.WarnMessage(e.Message, e.Channel, e.React, warnMessage);
        }
    }
}