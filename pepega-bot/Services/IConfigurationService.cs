using Microsoft.Extensions.Configuration;

namespace pepega_bot.Services
{
    public interface IConfigurationService
    {
        IConfiguration Configuration { get; }
    }
}