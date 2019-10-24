using Microsoft.Extensions.Configuration;

namespace pepega_bot.Services
{
    public class ConfigurationService : IConfigurationService
    {
        public IConfiguration Configuration { get; }

        public ConfigurationService(IConfiguration configuration)
        {
            Configuration = configuration;
        }
    }
}