using System;
using Discord;
using Microsoft.Extensions.Configuration;
using pepega_bot.Services;
using pepega_bot.Utils;

namespace pepega_bot.Module
{
    internal class JaraSoukupModule: IModule
    {
        private readonly IConfiguration _config;
        private readonly BaseWarnMessageModule _baseWarnMessageModule;
        private readonly string _jaraSoukupUserId;
        private readonly Emoji _rabbit2Emoji;


        public JaraSoukupModule(IConfigurationService configService, CommandHandlingService chService)
        {
            _config = configService.Configuration;
            _jaraSoukupUserId = _config["UserIds:JaraSoukup"];
            _rabbit2Emoji = new Emoji(_config["Emojis:Rabbit2"]);
            var blueCheckmark = new Emoji(_config["Emojis:BlueCheckmark"]);

            _baseWarnMessageModule = new BaseWarnMessageModule(configService, blueCheckmark);

            chService.ReactAdded += ReactionAddedAsync;
        }

        private async void ReactionAddedAsync(object sender, ReactionAddedEventArgs e)
        {
            if (!e.React.Emote.Equals(_rabbit2Emoji)) return;

            var reactInfo = new ReactInfo(_config, e.Message, e.Channel, e.React);
            var warnMessage =
                $"Master <@{_jaraSoukupUserId}>, your comrade <@{reactInfo.ReactAuthorId}> has found some *steamy* discussion. You should take a look." +
                Environment.NewLine + $"Discussion reference: {reactInfo.MessageLink}";

            await _baseWarnMessageModule.WarnMessage(e.Message, e.Channel, e.React, warnMessage);
        }
    }
}