using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using pepega_bot.Module;
using pepega_bot.Services;
using Quartz.Impl;

namespace pepega_bot
{
    static class Program
    {
        private static void Main(string[] args)
        {
            using (var dp = new DiscordProgram())
            {
                try
                {
                    dp.MainAsync().GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    dp.Logger.Error(ex);
                    throw;
                }
            }
        }
    }

    public class DiscordProgram: IDisposable
    {
        private readonly ConfigurationService _configService;
        private readonly ServiceProvider _services;
        private readonly DiscordSocketClient _client;
        private readonly List<IModule> _modules;
        public readonly NLog.ILogger Logger;

        private static ConfigurationService BuildConfigurationService()
        {
            return new ConfigurationService(
                new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("config.json")
                    .AddJsonFile("config.secret.json")
#if DEBUG
                    .AddJsonFile("config.dev.json")
#endif
                    .Build());
        }

        private static ServiceProvider BuildServiceProvider(IConfigurationService cs)
        {
            return new ServiceCollection()
                .AddSingleton<IConfigurationService>(cs)
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<HttpClient>()
                .AddSingleton<DatabaseService>()
                .AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                    loggingBuilder.AddNLog(cs.Configuration);
                })
                .BuildServiceProvider();
        }

        public DiscordProgram()
        {
            _configService = BuildConfigurationService();
            _services = BuildServiceProvider(_configService);
            _client = _services.GetRequiredService<DiscordSocketClient>();
            _modules = new List<IModule>();
            Logger = LogManager.GetCurrentClassLogger();
        }

        public async Task MainAsync()
        {
            _client.Log += Log;
            _services.GetRequiredService<CommandService>().Log += Log;

            await _client.LoginAsync(TokenType.Bot, _configService.Configuration["BotSecret"]);
            await _client.StartAsync();

            await _services.GetRequiredService<CommandHandlingService>().InitializeAsync();

            _client.GuildAvailable += OnGuildDataLoaded;

            var commandHandlingService = _services.GetRequiredService<CommandHandlingService>();
            var databaseService = _services.GetRequiredService<DatabaseService>();

            _modules.Add(new HamagenModule(_configService, commandHandlingService));
            _modules.Add(new JaraSoukupModule(_configService, commandHandlingService));
            _modules.Add(new PaprikaFilterModule(_configService, commandHandlingService, _client));
            _modules.Add(new VocabularyModule(databaseService, _configService, commandHandlingService));

            await Task.Delay(-1);
        }

        private async Task OnGuildDataLoaded(SocketGuild arg)
        {
            if (arg.Id != ulong.Parse(_configService.Configuration["RingFit:GuildId"]))
                return;

            var factory = new StdSchedulerFactory();
            var scheduler = await factory.GetScheduler();
            await scheduler.Start();

            var commandHandlingService = _services.GetRequiredService<CommandHandlingService>();
            var databaseService = _services.GetRequiredService<DatabaseService>();

            _modules.Add(new RingFitModule(databaseService, _configService.Configuration,
                commandHandlingService, _client, scheduler));
            _modules.Add(new TobikExposerModule(_configService, commandHandlingService, scheduler));
        }


        private Task Log(LogMessage msg)
        {
            Logger.Debug(msg.ToString());
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _services?.Dispose();
            _client?.Dispose();
        }
    }
}
