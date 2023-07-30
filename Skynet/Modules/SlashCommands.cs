namespace Skynet.Modules
{
    using Discord;
    using Discord.Interactions;
    using Discord.WebSocket;
    using Skynet.Extensions;
    using Skynet.Services;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    internal class SlashCommands : InteractionModuleBase<SocketInteractionContext<SocketSlashCommand>>
    {
        private readonly MusicService _musicService;
        private readonly DiscordSocketClient _client;

        public SlashCommands(MusicService musicService, DiscordSocketClient client)
        {
            _musicService = musicService;
            _client = client;
        }

        [SlashCommand("help", "Sends help")]
        public async Task Help()
        {
            await DeferAsync();

            var rand = new Random();
            var number = rand.Next(1, 4);
            if (number == 1)
            {
                await FollowupAsync(text: ":ambulance:");
            }
            else if (number == 2)
            {
                await FollowupAsync(text: ":police_car:");
            }
            else
            {
                await FollowupAsync(text: ":fire_engine:");
            }
        }

        [SlashCommand("play", "Searches for a song or playlist on Youtube and adds it to the queue")]
        public async Task Play([Summary(description: "Song title/url or playlist url")] string query)
        {
            await DeferAsync();

            var user = Context.User as SocketGuildUser;
            if (!await IsInVoiceChannel(user)) return;

            var botUser = await Context.Channel.GetUserAsync(Context.Client.CurrentUser.Id);
            if (botUser is IGuildUser bot)
            {
                if (bot.VoiceChannel == null || bot.VoiceChannel.Id != user!.VoiceChannel.Id)
                {
                    await Join();
                }
            }

            var message = await _client.GetMessageFromLink(query);
            var file = message?.Attachments?.FirstOrDefault();
            Embed embed;
            if (file != null)
            {
                embed = await _musicService.PlayAsync(file.Url, Context.Guild, user!, Context.Channel);
            }
            else
            {
                embed = await _musicService.PlayAsync(query, Context.Guild, user, Context.Channel);
            }

            await FollowupAsync(embed: embed);
        }

        [SlashCommand("stop", "Stops the current song and clears the queue")]
        public async Task Stop()
        {
            await DeferAsync();

            if (!await IsInSameVoiceChannelAsBot(Context.User as SocketGuildUser)) return;

            await FollowupAsync(embed: await _musicService.StopAsync(Context.Guild));
        }

        [SlashCommand("skip", "Skips the current song")]
        public async Task Skip()
        {
            await DeferAsync();

            if (!await IsInSameVoiceChannelAsBot(Context.User as SocketGuildUser)) return;

            await FollowupAsync(embed: await _musicService.SkipAsync(Context.Guild));
        }

        [SlashCommand("queue", "Lists all songs in the queue")]
        public async Task Queue()
        {
            await DeferAsync();

            if (!await IsInSameVoiceChannelAsBot(Context.User as SocketGuildUser)) return;

            await FollowupAsync(embed: _musicService.ListQueue(Context.Guild));
        }

        [SlashCommand("pause", "Pauses the current song")]
        public async Task Pause()
        {
            await DeferAsync();

            if (!await IsInSameVoiceChannelAsBot(Context.User as SocketGuildUser)) return;

            (var embed, _) = await _musicService.PauseOrResumeAsync(Context.Guild);

            await FollowupAsync(embed: embed);
        }

        [SlashCommand("resume", "Resumes the current song")]
        public async Task Resume()
        {
            await DeferAsync();

            if (!await IsInSameVoiceChannelAsBot(Context.User as SocketGuildUser)) return;

            await FollowupAsync(embed: await _musicService.ResumeAsync(Context.Guild));
        }

        [SlashCommand("seek", "Seeks to the specified position in current song")]
        public async Task Seek([Summary(description: "Set currently playing song to this point in time")] string time)
        {
            await DeferAsync();

            if (!await IsInSameVoiceChannelAsBot(Context.User as SocketGuildUser)) return;

            time = "00:00:" + time;

            if (TimeSpan.TryParse(time, out var timeSpan))
            {
                await FollowupAsync(embed: await _musicService.Seek(Context.Guild, seekPosition: timeSpan));
            }
            else
            {
                var embed = new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithDescription("Invalid time format")
                    .Build();

                await FollowupAsync(embed: embed);
            }
        }

        [SlashCommand("shuffle", "Shuffles the queue")]
        public async Task Shuffle()
        {
            await DeferAsync();

            if (!await IsInSameVoiceChannelAsBot(Context.User as SocketGuildUser)) return;

            await FollowupAsync(embed: _musicService.Shuffle(Context.Guild));
        }

        [SlashCommand("volume", "Sets playback volume, min - 0, max - 1000")]
        public async Task Volume([Summary(description: "Volume, min: 1, max: 1000, normal: 100")] int volume)
        {
            await DeferAsync();

            if (!await IsInSameVoiceChannelAsBot(Context.User as SocketGuildUser)) return;

            await FollowupAsync(embed: await _musicService.SetVolumeAsync(volume, Context.Guild));
        }

        [SlashCommand("join", "Joins your voice channel")]
        public async Task Join()
        {
            var user = Context.User as SocketGuildUser;
            if (!await IsInVoiceChannel(user)) return;

            var channel = Context.Channel as ITextChannel;
            await _musicService.ConnectAsync(user!.VoiceChannel, channel!);
            var embed = new EmbedBuilder()
                .WithColor(Color.Blue)
                .WithDescription($"Connected to {user.VoiceChannel.Name}")
                .Build();

            await FollowupAsync(embed: embed);

        }

        [SlashCommand("leave", "Leaves your voice channel")]
        public async Task Leave()
        {
            await DeferAsync();

            var user = Context.User as SocketGuildUser;
            if (!await IsInSameVoiceChannelAsBot(user)) return;

            await _musicService.LeaveAsync(user!.VoiceChannel);
            var embed = new EmbedBuilder()
                .WithColor(Color.Blue)
                .WithDescription($"Bot has now left {user.VoiceChannel.Name}")
                .Build();
            await FollowupAsync(embed: embed);
        }

        [SlashCommand("cock", "Plays no cock like horse cock")]
        public async Task Cock()
        {
            await DeferAsync();

            var user = Context.User as SocketGuildUser;
            if (!await IsInVoiceChannel(user)) return;

            var botUser = await Context.Channel.GetUserAsync(Context.Client.CurrentUser.Id);
            if (botUser is IGuildUser bot)
            {
                if (bot.VoiceChannel == null || bot.VoiceChannel.Id != user.VoiceChannel.Id)
                {
                    await Join();
                }
            }

            var embed = await _musicService.PlayAsync("no cock like horse cock", Context.Guild, user, Context.Channel);

            await FollowupAsync(embed: embed);
        }

        [SlashCommand("showui", "Shows the UI in case it disappeared, surely that will never happen though")]
        public async Task ShowUi()
        {
            await DeferAsync();

            if (!await IsInSameVoiceChannelAsBot(Context.User as SocketGuildUser)) return;

            await FollowupAsync(embed: await _musicService.ShowUi(Context.Guild));
        }

        [SlashCommand("bass", "Boosts bass")]
        public async Task Bass([Summary(description: "Intensity, normal: 0, max: 100")] int intensity)
        {
            await DeferAsync();

            if (!await IsInSameVoiceChannelAsBot(Context.User as SocketGuildUser)) return;

            await FollowupAsync(embed: await _musicService.Bass(Context.Guild, intensity * 0.01));
        }

        private async Task<bool> IsInVoiceChannel(SocketGuildUser? user)
        {
            if (user?.VoiceChannel == null)
            {
                var embed = new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithDescription("You need to connect to a voice channel to use this command")
                    .Build();
                await FollowupAsync(text: $"{user!.Mention}", embed: embed, ephemeral: true);

                return false;
            }

            return true;
        }

        private async Task<bool> IsInSameVoiceChannelAsBot(SocketGuildUser? user)
        {
            if (!await IsInVoiceChannel(user)) return false;

            var botUser = await Context.Channel.GetUserAsync(Context.Client.CurrentUser.Id);
            if (botUser is IGuildUser bot)
            {
                if (bot.VoiceChannel?.Id != user!.VoiceChannel.Id)
                {
                    var embed = new EmbedBuilder()
                        .WithColor(Color.Red)
                        .WithDescription("You have to be in the same voice channel as bot to use this command")
                        .Build();
                    await FollowupAsync(text: $"{user.Mention}", embed: embed, ephemeral: true);

                    return false;
                }
            }

            return true;
        }
    }
}
