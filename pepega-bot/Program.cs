using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using pepega_bot.Module;
using pepega_bot.Services;

namespace pepega_bot
{
    class Program
    {
        static void Main(string[] args)
        {
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        private DiscordSocketClient _client;

        private async Task MainAsync()
        {
            var configService = new ConfigurationService(
                new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("config.json")
                    .AddJsonFile("config.secret.json")
#if DEBUG
                    .AddJsonFile("config.dev.json")
#endif
                    .Build());
            using (var services = ConfigureServices(configService))
            {
                _client = services.GetRequiredService<DiscordSocketClient>();

                _client.Log += Log;
                services.GetRequiredService<CommandService>().Log += Log;

                await _client.LoginAsync(TokenType.Bot, configService.Configuration["BotSecret"]);
                await _client.StartAsync();

                await services.GetRequiredService<CommandHandlingService>().InitializeAsync();

                var commandHandlingService = services.GetRequiredService<CommandHandlingService>();
                var databaseService = services.GetRequiredService<DatabaseService>();

                Thread.Sleep(TimeSpan.FromSeconds(5)); // ugly hack - wait for Guild Data to load up

                var hamagenModule = new HamagenModule(configService, commandHandlingService);
                var jaraSoukupModule = new JaraSoukupModule(configService, commandHandlingService);
                var paprikaModule = new PaprikaFilterModule(configService, commandHandlingService, _client);
                var vocabularyModule = new VocabularyModule(databaseService, configService, commandHandlingService);
                var ringFitModule = new RingFitModule(databaseService, configService.Configuration, commandHandlingService, _client);

                await Task.Delay(-1);
            }
        }

        private ServiceProvider ConfigureServices(IConfigurationService config)
        {
            return new ServiceCollection()
                .AddSingleton<IConfigurationService>(config)
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<HttpClient>()
                .AddSingleton<DatabaseService>()
                .BuildServiceProvider();
        }

        private static Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
