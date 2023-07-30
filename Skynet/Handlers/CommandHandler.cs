namespace Skynet.Handlers
{
    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;
    using Skynet.Entities;
    using System;
    using System.Reflection;
    using System.Threading.Tasks;

    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _cmdService;
        private readonly Config _config;
        private readonly IServiceProvider _services;

        public CommandHandler(DiscordSocketClient client, CommandService cmdService, Config config, IServiceProvider services)
        {
            _client = client;
            _cmdService = cmdService;
            _config = config;
            _services = services;
        }

        public async Task InitializeAsync()
        {
            await _cmdService.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            _cmdService.Log += LogAsync;
            _client.MessageReceived += HandleMessageAsync;
        }

        private async Task HandleMessageAsync(SocketMessage socketMessage)
        {
            var argPos = 0;
            if (socketMessage.Author.IsBot) return;

            if (socketMessage is not SocketUserMessage userMessage)
                return;

            if (!userMessage.HasStringPrefix($"{_config.CommandPrefix}", ref argPos))
                return;

            var context = new SocketCommandContext(_client, userMessage);
            _ = await _cmdService.ExecuteAsync(context, argPos, _services);
        }

        private Task LogAsync(LogMessage logMessage)
        {
            if (logMessage.Exception != null)
            {
                Console.WriteLine(logMessage.Exception.Message);
            }

            Console.WriteLine(logMessage.Message);
            return Task.CompletedTask;
        }
    }
}
