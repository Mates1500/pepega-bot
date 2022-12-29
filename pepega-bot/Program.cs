using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Net.Http;
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
using pepega_bot.Scheduling;
using pepega_bot.Services;
using Quartz.Impl;

namespace pepega_bot
{
    static class Program
    {
        private static void Main()
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

        private readonly ServiceContainer _jobContainer;
        private bool _postGuildDataInitializationDone;

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
            var clientConfig = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
            };

            var client = new DiscordSocketClient(clientConfig);

            return new ServiceCollection()
                .AddSingleton<IConfigurationService>(cs)
                .AddSingleton<DiscordSocketClient>(client)
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
            _jobContainer = new ServiceContainer();
            _client = _services.GetRequiredService<DiscordSocketClient>();
            _modules = new List<IModule>();
            Logger = LogManager.GetCurrentClassLogger();

            _postGuildDataInitializationDone = false;
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
            if (_postGuildDataInitializationDone) // this function may get called multiple times due to reconnects otherwise
                return;

            if (arg.Id != ulong.Parse(_configService.Configuration["RingFit:GuildId"]))
                return;

            var factory = new StdSchedulerFactory();
            var scheduler = await factory.GetScheduler();
            await scheduler.Start();

            var commandHandlingService = _services.GetRequiredService<CommandHandlingService>();
            var databaseService = _services.GetRequiredService<DatabaseService>();

            var jobFactory = new JobFactory(_jobContainer);
            scheduler.JobFactory = jobFactory;

            _modules.Add(new RingFitModule(_configService, commandHandlingService, _client, scheduler, _jobContainer));
            _modules.Add(new TobikExposerModule(_configService, commandHandlingService, scheduler, _jobContainer));
            _modules.Add(new YukiiModule(_configService, commandHandlingService, _client, scheduler, _jobContainer));

            _postGuildDataInitializationDone = true;
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
