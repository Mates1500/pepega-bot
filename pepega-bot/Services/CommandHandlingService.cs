using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace pepega_bot.Services
{
    internal class CommandHandlingService
    {
        private readonly IServiceProvider _services;
        private readonly CommandService _commands;

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
        public event EventHandler<MessageUpdatedEventArgs> MessageUpdated;
        public event EventHandler<MessageRemovedEventArgs> MessageRemoved;
        public event EventHandler<ReactionAddedEventArgs> ReactAdded;
        public event EventHandler<ReactionRemovedEventArgs> ReactRemoved;

        public CommandHandlingService(IServiceProvider services, CommandService commandService, DiscordSocketClient discordSocketClient)
        {
            _services = services;
            _commands = commandService;

            discordSocketClient.MessageReceived += MessageReceivedAsync;
            discordSocketClient.MessageUpdated += MessageUpdatedAsync;
            discordSocketClient.MessageDeleted += MessageDeletedAsync;
            discordSocketClient.ReactionAdded += ReactionAddedAsync;
            discordSocketClient.ReactionRemoved += ReactionRemovedAsync;
        }

        private Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var tempCopy = ReactAdded;
            var args = new ReactionAddedEventArgs(message, channel, reaction);
            tempCopy?.Invoke(this, args);
            return Task.CompletedTask;
        }

        private Task ReactionRemovedAsync(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var tempCopy = ReactRemoved;
            var args = new ReactionRemovedEventArgs(message, channel, reaction);
            tempCopy?.Invoke(this, args);
            return Task.CompletedTask;
        }

        private Task MessageUpdatedAsync(Cacheable<IMessage, ulong> oldMessage, SocketMessage newMessage, ISocketMessageChannel channel)
        {
            var tempCopy = MessageUpdated;
            var args = new MessageUpdatedEventArgs(oldMessage, newMessage, channel);
            tempCopy?.Invoke(this, args);
            return Task.CompletedTask;
        }

        private Task MessageReceivedAsync(SocketMessage message)
        {
            var tempCopy = MessageReceived;
            var args = new MessageReceivedEventArgs(message);
            tempCopy?.Invoke(this, args);
            return Task.CompletedTask;
        }

        private Task MessageDeletedAsync(Cacheable<IMessage, ulong> message, ISocketMessageChannel channel)
        {
            var tempCopy = MessageRemoved;
            var args = new MessageRemovedEventArgs(message, channel);
            tempCopy?.Invoke(this, args);
            return Task.CompletedTask;
        }

        public async Task InitializeAsync()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }
    }

}