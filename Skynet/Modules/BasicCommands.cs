namespace Skynet.Modules
{
    using Discord;
    using Discord.Commands;
    using Skynet.Entities;
    using System;
    using System.Threading.Tasks;

    public class BasicCommands : ModuleBase<SocketCommandContext>
    {
        private readonly CommandService _commandService;
        private readonly Config _config;

        public BasicCommands(CommandService commandService, Config config)
        {
            _commandService = commandService;
            _config = config;
        }

        [Command("help")]
        [Summary("Sends help")]
        public async Task Help()
        {
            var rand = new Random();
            var number = rand.Next(1, 4);
            if (number == 1)
            {
                await ReplyAsync(":ambulance:");
            }
            else if (number == 2)
            {
                await ReplyAsync(":police_car:");
            }
            else
            {
                await ReplyAsync(":fire_engine:");
            }
        }

        [Command("commands")]
        [Summary("Displays a list of all commands")]
        public async Task Commands()
        {
            //this embed will fail if summary is not set for every command
            var embed = new EmbedBuilder()
                .WithColor(Color.Blue)
                .WithTitle("Available commands");
            foreach (var command in _commandService.Commands)
            {
                embed.AddField($"{_config.CommandPrefix}{command.Name}", command.Summary);
            }

            await ReplyAsync(embed: embed.Build());
        }
    }
}
