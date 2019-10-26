﻿using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using pepega_bot.Module;

namespace pepega_bot.Services
{
    internal class CommandHandlingService
    {
        private readonly IServiceProvider _services;
        private readonly IConfiguration _config;
        private readonly IHamagenModule _hamagenModule;
        private readonly IPaprikaFilterModule _paprikaFilterModule;
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _discord;

        public CommandHandlingService(IServiceProvider services)
        {
            _services = services;
            _config = _services.GetRequiredService<IConfigurationService>().Configuration;
            _discord = _services.GetRequiredService<DiscordSocketClient>();
            _commands = _services.GetRequiredService<CommandService>();
            _hamagenModule = _services.GetRequiredService<IHamagenModule>();
            _paprikaFilterModule = _services.GetRequiredService<IPaprikaFilterModule>();

            _discord.MessageReceived += MessageReceivedAsync;
            _discord.MessageUpdated += MessageUpdatedAsync;
            _discord.ReactionAdded += ReactionAddedAsync;
        }

        public async Task InitializeAsync()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        private async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction react)
        {
            Emote.TryParse(_config["Emotes:CallHamagen"], out var hamagenEmote);
            
            if (react.Emote.Equals(hamagenEmote))
            {
                await _hamagenModule.HandleHamagenEmoteReact(message, channel, react);
            }
        }

        private async Task MessageReceivedAsync(SocketMessage message)
        {
            if (message.Author.Id == ulong.Parse(_config["UserIds:Paprika"]))
            {
                await _paprikaFilterModule.HandlePaprikaMessage(message);
            }
        }

        private async Task MessageUpdatedAsync(Cacheable<IMessage, ulong> originalMessage, SocketMessage newMessage, ISocketMessageChannel channel)
        {
            if (newMessage.Author.Id == ulong.Parse(_config["UserIds:Paprika"]))
            {
                await _paprikaFilterModule.HandlePaprikaMessage(newMessage);
            }
        }
    }
}