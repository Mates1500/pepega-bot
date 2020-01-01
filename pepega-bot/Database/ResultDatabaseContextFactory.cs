using System.IO;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using pepega_bot.Services;

namespace pepega_bot.Module
{
    // For DB migration creation through 'dotnet ef' CLI
    internal class ResultDatabaseContextFactory : IDesignTimeDbContextFactory<ResultDatabaseContext>
    {
        public ResultDatabaseContext CreateDbContext(string[] args)
        {
            var configService = new ConfigurationService(new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("config.json").Build());
            var config = new ConfigurationService(configService.Configuration);

            return new ResultDatabaseContext(config, false);
        }
    }
}