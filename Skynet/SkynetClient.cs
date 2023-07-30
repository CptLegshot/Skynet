namespace Skynet
{
    using Discord;
    using Discord.Commands;
    using Discord.Interactions;
    using Discord.WebSocket;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Skynet.Entities;
    using Skynet.Handlers;
    using Skynet.Services;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;
    using Victoria;

    public class SkynetClient
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _cmdService;
        private readonly InteractionService _interactionService;
        private IServiceProvider _services;
        private readonly LogService _logService;
        private readonly ConfigService _configService;
        private readonly Config _config;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public SkynetClient()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                AlwaysDownloadUsers = true,
                MessageCacheSize = 50,
                LogLevel = LogSeverity.Debug,
                GatewayIntents = GatewayIntents.MessageContent | GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.GuildVoiceStates
            });

            _cmdService = new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Verbose,
                CaseSensitiveCommands = false
            });

            _interactionService = new InteractionService(_client, new InteractionServiceConfig
            {
                LogLevel = LogSeverity.Verbose,
            });

            _logService = new LogService();
            _configService = new ConfigService();
            _config = _configService.GetConfig();
        }

        public async Task InitializeAsync()
        {
            Process lavalinkProcess = new();
            lavalinkProcess.StartInfo.UseShellExecute = false;
            lavalinkProcess.StartInfo.RedirectStandardOutput = true;
            lavalinkProcess.StartInfo.FileName = "java";
            lavalinkProcess.StartInfo.WorkingDirectory = $"{Directory.GetCurrentDirectory()}/Lavalink/";
            lavalinkProcess.StartInfo.Arguments = $"-jar {Directory.GetCurrentDirectory()}/Lavalink/Lavalink.jar";

            TaskCompletionSource<bool> lavalinkProcessStart = new();
            lavalinkProcess.OutputDataReceived += (sender, args) =>
            {
                Console.WriteLine(args.Data);
                if (args.Data != null && args.Data.Contains("ready to accept connections", StringComparison.OrdinalIgnoreCase))
                {
                    _ = lavalinkProcessStart.TrySetResult(true);
                }
            };

            lavalinkProcess.Start();
            lavalinkProcess.BeginOutputReadLine();

            var token = Environment.GetEnvironmentVariable("token", EnvironmentVariableTarget.Process);
            token ??= Environment.GetEnvironmentVariable("token", EnvironmentVariableTarget.User);
            token ??= Environment.GetEnvironmentVariable("token", EnvironmentVariableTarget.Machine);

            token ??= _config.Token;

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException("Discord token missing, set it in Config.json or as a 'token' env variable");
            }

            await lavalinkProcessStart.Task;
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
            await _client.SetGameAsync($"{_config.CommandPrefix}commands");
            _client.Log += LogAsync;

            AppDomain.CurrentDomain.ProcessExit += async (sender, args) =>
            {
                foreach (var guild in _client.Guilds)
                {
                    if (guild.CurrentUser.VoiceChannel != null)
                    {
                        await guild.CurrentUser.VoiceChannel.DisconnectAsync();
                    }
                }
                await _client.StopAsync();
                lavalinkProcess.Kill();
            };

            _services = SetupServices();

            await _services.GetRequiredService<CommandHandler>().InitializeAsync();
            await _services.GetRequiredService<InteractionHandler>().InitializeAsync();
            await _services.GetRequiredService<MusicService>().InitializeAsync();

            await Task.Delay(-1);
        }

        private async Task LogAsync(LogMessage logMessage) => await _logService.LogAsync(logMessage);

        private IServiceProvider SetupServices()
            => new ServiceCollection()
            .AddLogging(x => x.AddConsole().SetMinimumLevel(LogLevel.Debug))
            .AddSingleton(_client)
            .AddSingleton(_cmdService)
            .AddSingleton(_interactionService)
            .AddSingleton(_logService)
            .AddSingleton(_config)
            .AddSingleton<CommandHandler>()
            .AddSingleton<InteractionHandler>()
            .AddLavaNode()
            .AddSingleton<MusicService>()
            .BuildServiceProvider();
    }
}
