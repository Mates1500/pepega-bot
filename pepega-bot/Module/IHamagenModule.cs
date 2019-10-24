using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace pepega_bot.Module
{
    public interface IHamagenModule
    {
        Task HandleHamagenEmoteReact(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction react);
    }
}