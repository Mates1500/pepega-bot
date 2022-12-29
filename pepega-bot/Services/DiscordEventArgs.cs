using System;
using Discord;
using Discord.WebSocket;

namespace pepega_bot.Services
{
    public class ReactionAddedEventArgs : EventArgs
    {
        public Cacheable<IUserMessage, ulong> Message { get; }
        public Cacheable<IMessageChannel, ulong> Channel { get; }
        public SocketReaction React { get; }

        public ReactionAddedEventArgs(Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, SocketReaction react)
        {
            Message = message;
            Channel = channel;
            React = react;
        }
    }

    internal class ReactionRemovedEventArgs
    {
        public Cacheable<IUserMessage, ulong> Message { get; }
        public Cacheable<IMessageChannel, ulong> Channel { get; }
        public SocketReaction React { get; }

        public ReactionRemovedEventArgs(Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, SocketReaction react)
        {
            Message = message;
            Channel = channel;
            React = react;
        }
    }

    public class MessageReceivedEventArgs : EventArgs
    {
        public SocketMessage Message { get; }

        public MessageReceivedEventArgs(SocketMessage message)
        {
            Message = message;
        }
    }

    public class MessageUpdatedEventArgs : EventArgs
    {
        public Cacheable<IMessage, ulong> Message { get; }
        public SocketMessage NewMessage { get; }
        public ISocketMessageChannel Channel { get; }

        public MessageUpdatedEventArgs(Cacheable<IMessage, ulong> message, SocketMessage newMessage, ISocketMessageChannel channel)
        {
            Message = message;
            NewMessage = newMessage;
            Channel = channel;
        }
    }

    internal class MessageRemovedEventArgs
    {
        public Cacheable<IMessage, ulong> Message { get; }
        public Cacheable<IMessageChannel, ulong> Channel { get; }

        public MessageRemovedEventArgs(Cacheable<IMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel)
        {
            Message = message;
            Channel = channel;
        }
    }
}