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
            if(tempCopy == null)
                return Task.CompletedTask;

            var args = new ReactionAddedEventArgs(message, channel, reaction);
            return Task.Run(() =>
            {
                tempCopy.Invoke(this, args);
            });
        }

        private Task ReactionRemovedAsync(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var tempCopy = ReactRemoved;
            if (tempCopy == null)
                return Task.CompletedTask;

            var args = new ReactionRemovedEventArgs(message, channel, reaction);
            return Task.Run(() =>
            {
                tempCopy.Invoke(this, args);
            });
        }

        private Task MessageUpdatedAsync(Cacheable<IMessage, ulong> oldMessage, SocketMessage newMessage, ISocketMessageChannel channel)
        {
            var tempCopy = MessageUpdated;
            if (tempCopy == null)
                return Task.CompletedTask;

            var args = new MessageUpdatedEventArgs(oldMessage, newMessage, channel);
            return Task.Run(() =>
            {
                tempCopy.Invoke(this, args);
            });
        }

        private Task MessageReceivedAsync(SocketMessage message)
        {
            var tempCopy = MessageReceived;
            if (tempCopy == null)
                return Task.CompletedTask;

            var args = new MessageReceivedEventArgs(message);
            return Task.Run(() =>
            {
                tempCopy.Invoke(this, args);
            });
        }

        private Task MessageDeletedAsync(Cacheable<IMessage, ulong> message, ISocketMessageChannel channel)
        {
            var tempCopy = MessageRemoved;
            if (tempCopy == null)
                return Task.CompletedTask;

            var args = new MessageRemovedEventArgs(message, channel);
            return Task.Run(() =>
            {
                tempCopy.Invoke(this, args);
            });
        }

        public async Task InitializeAsync()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }
    }

}