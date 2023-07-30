namespace Skynet.Handlers
{
    using Discord;
    using Discord.Interactions;
    using Discord.WebSocket;
    using Skynet.Extensions;
    using Skynet.Modules;
    using Skynet.Services;
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    internal class InteractionHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _interactionService;
        private readonly IServiceProvider _services;
        private readonly MusicService _musicService;

        public InteractionHandler(InteractionService interactionService, DiscordSocketClient client, IServiceProvider services, MusicService musicService)
        {
            _interactionService = interactionService;
            _client = client;
            _services = services;
            _musicService = musicService;
        }

        public async Task InitializeAsync()
        {
            await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            await _interactionService.AddModuleAsync<SlashCommands>(_services);
            await _interactionService.AddModuleAsync<ModalInteractions>(_services);
            _client.Ready += async () => await _interactionService.RegisterCommandsGloballyAsync();

            _interactionService.Log += LogAsync;
            _client.ButtonExecuted += HandleButtonAsync;
            _client.SlashCommandExecuted += HandleSlashCommandAsync;
            _client.ModalSubmitted += HandleModalAsync;
        }

        private async Task HandleModalAsync(SocketModal modal)
        {
            var context = new SocketInteractionContext(_client, modal);

            //Should use this but ModalInteractions module is not working for some reason
            //await _interactionService.ExecuteCommandAsync(context, _services);

            //temp
            await context.Interaction.DeferAsync();

            if (modal.Data.CustomId == "add")
            {
                var query = modal.Data.Components.Single(x => x.CustomId == "query").Value;

                var message = await _client.GetMessageFromLink(query);
                var file = message?.Attachments?.FirstOrDefault();
                Embed embed;
                if (file != null)
                {
                    embed = await _musicService.PlayAsync(file.Url, context.Guild, modal.User, context.Channel);
                }
                else
                {
                    embed = await _musicService.PlayAsync(query, context.Guild, context.User, context.Channel);
                }

                await context.Interaction.FollowupAsync(embed: embed);

                return;
            }
            //temp
        }

        private async Task HandleButtonAsync(SocketMessageComponent socketComponent)
        {
            var context = new SocketInteractionContext<SocketMessageComponent>(_client, socketComponent);

            await _interactionService.ExecuteCommandAsync(context, _services);
        }

        private async Task HandleSlashCommandAsync(SocketSlashCommand command)
        {
            var context = new SocketInteractionContext<SocketSlashCommand>(_client, command);

            await _interactionService.ExecuteCommandAsync(context, _services);
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
