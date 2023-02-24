using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using pepega_bot.InteractionModule;
using pepega_bot.Module;
using pepega_bot.Scheduling;
using pepega_bot.Services;
using Quartz;
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
        private readonly InteractionService _interactionService;
        private readonly List<IModule> _modules;
        public readonly NLog.ILogger Logger;

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

        private static async Task<ServiceProvider> BuildServiceProvider(IConfigurationService cs, 
            IServiceContainer serviceContainer)
        {
            var clientConfig = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
            };

            var client = new DiscordSocketClient(clientConfig);

            var schedulerFactory = new StdSchedulerFactory();
            var scheduler = await schedulerFactory.GetScheduler();
            await scheduler.Start();

            var jobFactory = new JobFactory(serviceContainer);
            scheduler.JobFactory = jobFactory;

            return new ServiceCollection()
                .AddSingleton<IConfigurationService>(cs)
                .AddSingleton<DiscordSocketClient>(client)
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<HttpClient>()
                .AddSingleton<ResultDatabaseContext>()
                .AddSingleton<DatabaseService>()
                .AddSingleton<IServiceContainer>(serviceContainer)
                .AddSingleton<IScheduler>(scheduler)
                .AddSingleton<RingFitModule>()
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
            var quartzJobContainer = new ServiceContainer();
            _services = BuildServiceProvider(_configService, quartzJobContainer).Result;

            _client = _services.GetRequiredService<DiscordSocketClient>();
            _interactionService = new InteractionService(_client);

            _modules = new List<IModule>();
            Logger = LogManager.GetCurrentClassLogger();

            _postGuildDataInitializationDone = false;
        }

        public async Task MainAsync()
        {
            _client.Log += Log;
            _services.GetRequiredService<CommandService>().Log += Log;
            _interactionService.Log += Log;

            _client.InteractionCreated += async (x) =>
            {
                var ctx = new SocketInteractionContext(_client, x);
                await _interactionService.ExecuteCommandAsync(ctx, _services);
            };

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

        private void WarmUpServices()
        // should be called only once
        // forces Service constructors to execute at least once, basically to initialize background tasks and event listeners
        {
            _services.GetRequiredService<RingFitModule>();
        }

        private async Task OnGuildDataLoaded(SocketGuild arg)
        {
            if (_postGuildDataInitializationDone) // this function may get called multiple times due to reconnects otherwise
                return;

            if (arg.Id != ulong.Parse(_configService.Configuration["RingFit:GuildId"]))
                return;


            var commandHandlingService = _services.GetRequiredService<CommandHandlingService>();
            var scheduler = _services.GetRequiredService<IScheduler>();
            var quartzJobContainer = _services.GetRequiredService<IServiceContainer>();
            var databaseService = _services.GetRequiredService<DatabaseService>();

            WarmUpServices();

            await _interactionService.AddModuleAsync<RingFitInteractionModule>(_services); // module discovery in assembly does not seem to work


            _modules.Add(new TobikExposerModule(_configService, commandHandlingService, scheduler, quartzJobContainer));
            _modules.Add(new YukiiModule(_configService, commandHandlingService, _client, scheduler, quartzJobContainer,
                databaseService));

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
