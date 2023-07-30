namespace Skynet.Modules
{
    using Discord;
    using Discord.Interactions;
    using Discord.WebSocket;
    using Skynet.Services;
    using Skynet.UI;
    using System;
    using System.Threading.Tasks;

    public class ButtonInteractions : InteractionModuleBase<SocketInteractionContext<SocketMessageComponent>>
    {
        private readonly MusicService _musicService;

        public ButtonInteractions(MusicService musicService)
        {
            _musicService = musicService;
        }

        [ComponentInteraction("play")]
        public async Task PlayAsync()
        {
            if (!await IsUserInSameVoiceChannelAsBot()) return;

            (var embed, var playerState) = await _musicService.PauseOrResumeAsync(Context.Guild);
            if (playerState == Victoria.Player.PlayerState.Playing)
            {
                await Context.Interaction.UpdateAsync(x => x.Components = ComponentUI.WithPauseButton);
            }
            else
            {
                await Context.Interaction.RespondAsync(embed: embed);
            }
        }

        [ComponentInteraction("pause")]
        public async Task PauseAsync()
        {
            if (!await IsUserInSameVoiceChannelAsBot()) return;

            (var embed, var playerState) = await _musicService.PauseOrResumeAsync(Context.Guild);
            if (playerState == Victoria.Player.PlayerState.Paused)
            {
                await Context.Interaction.UpdateAsync(x => x.Components = ComponentUI.WithPlayButton);
            }
            else
            {
                await Context.Interaction.RespondAsync(embed: embed);
            }
        }

        [ComponentInteraction("add")]
        public async Task AddAsync()
        {
            if (!await IsUserInSameVoiceChannelAsBot()) return;

            var modal = new ModalBuilder()
                .WithCustomId("add")
                .AddTextInput(label: "Song name/url or playlist url", customId: "query", required: true)
                .WithTitle("Add a song/playlist to the queue")
                .Build();

            await Context.Interaction.RespondWithModalAsync(modal);
        }

        [ComponentInteraction("listQueue")]
        public async Task ListQueueAsync()
        {
            await Context.Interaction.DeferAsync();
            if (!await IsUserInSameVoiceChannelAsBot()) return;

            await Context.Interaction.FollowupAsync(embed: _musicService.ListQueue(Context.Guild));
        }

        [ComponentInteraction("stop")]
        public async Task StopAsync()
        {
            await Context.Interaction.DeferAsync();
            if (!await IsUserInSameVoiceChannelAsBot()) return;

            _ = await _musicService.StopAsync(Context.Guild);
        }

        [ComponentInteraction("fastForward")]
        public async Task FastForwardAsync()
        {
            await Context.Interaction.DeferAsync();
            if (!await IsUserInSameVoiceChannelAsBot()) return;

            _ = await _musicService.Seek(Context.Guild, forwardSec: 30);
        }

        [ComponentInteraction("fastReverse")]
        public async Task FastReverseAsync()
        {
            await Context.Interaction.DeferAsync();
            if (!await IsUserInSameVoiceChannelAsBot()) return;

            _ = await _musicService.Seek(Context.Guild, reverseSec: 30);
        }

        [ComponentInteraction("previous")]
        public async Task PreviousAsync()
        {
            await Context.Interaction.DeferAsync();
            if (!await IsUserInSameVoiceChannelAsBot()) return;

            _ = await _musicService.Seek(Context.Guild, seekPosition: new TimeSpan());
        }

        [ComponentInteraction("skip")]
        public async Task SkipsAsync()
        {
            await Context.Interaction.DeferAsync();
            if (!await IsUserInSameVoiceChannelAsBot()) return;

            _ = await _musicService.SkipAsync(Context.Guild);
        }

        [ComponentInteraction("shuffle")]
        public async Task Shuffle()
        {
            await Context.Interaction.DeferAsync();
            if (!await IsUserInSameVoiceChannelAsBot()) return;

            await Context.Interaction.FollowupAsync(embed: _musicService.Shuffle(Context.Guild));
        }

        private async Task<bool> IsUserInSameVoiceChannelAsBot()
        {
            var user = Context.User as SocketGuildUser;
            if (user?.VoiceChannel == null)
            {
                var embed = new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithDescription("You need to connect to a voice channel to use this button")
                    .Build();
                await FollowupAsync(text: $"{user?.Mention}", embed: embed, ephemeral: true);

                return false;
            }

            var botUser = await Context.Channel.GetUserAsync(Context.Client.CurrentUser.Id);
            if (botUser is IGuildUser bot)
            {
                if (bot.VoiceChannel?.Id != user.VoiceChannel.Id)
                {
                    var embed = new EmbedBuilder()
                        .WithColor(Color.Red)
                        .WithDescription("You have to be in the same voice channel as bot to use this button")
                        .Build();
                    await FollowupAsync(text: $"{user.Mention}", embed: embed, ephemeral: true);

                    return false;
                }
            }

            return true;
        }
    }
}
