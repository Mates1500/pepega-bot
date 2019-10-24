using System.Threading.Tasks;
using Discord.WebSocket;

namespace pepega_bot.Module
{
    public interface IPaprikaFilterModule
    {
        Task HandlePaprikaMessage(SocketMessage message);
    }
}