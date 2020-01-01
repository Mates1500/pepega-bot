using Microsoft.Extensions.Configuration;

namespace pepega_bot.Services
{
    public class ConfigurationService : IConfigurationService
    {
        public IConfiguration Configuration { get; }
        public string SqliteDbLocation => Configuration["SqliteDbLocation"];
        public string SqliteDbConnectionString => $"Data Source={SqliteDbLocation}";

        public ConfigurationService(IConfiguration configuration)
        {
            Configuration = configuration;
        }
    }
}