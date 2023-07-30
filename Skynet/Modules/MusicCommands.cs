namespace Skynet.Modules
{
    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;
    using Skynet.Extensions;
    using Skynet.Services;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    public class MusicCommands : ModuleBase<SocketCommandContext>
    {
        private readonly MusicService _musicService;
        private readonly DiscordSocketClient _client;

        public MusicCommands(MusicService musicService, DiscordSocketClient client)
        {
            _musicService = musicService;
            _client = client;
        }

        [Command("play")]
        [Summary("Searches for a song or playlist on Youtube and adds it to the queue")]
        public async Task Play([Remainder] string query = "")
        {
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

            Embed embed;
            IAttachment? file = Context.Message.Attachments?.FirstOrDefault();
            var message = await _client.GetMessageFromLink(query);

            if (message != null && file == null)
            {
                file = message.Attachments?.FirstOrDefault();
            }

            if (file != null && file.ContentType == "audio/mpeg")
            {
                embed = await _musicService.PlayAsync(file.Url, Context.Guild, user!, Context.Channel);
            }
            else
            {
                embed = await _musicService.PlayAsync(query, Context.Guild, user!, Context.Channel);
            }

            await ReplyAsync(embed: embed);
        }

        [Command("stop")]
        [Summary("Stops the current song and clears the queue")]
        public async Task Stop()
        {
            if (!await IsInSameVoiceChannelAsBot(Context.User as SocketGuildUser)) return;

            await ReplyAsync(embed: await _musicService.StopAsync(Context.Guild));
        }

        [Command("skip")]
        [Summary("Skips the current song")]
        public async Task Skip()
        {
            if (!await IsInSameVoiceChannelAsBot(Context.User as SocketGuildUser)) return;

            await ReplyAsync(embed: await _musicService.SkipAsync(Context.Guild));
        }

        [Command("queue")]
        [Summary("Lists all songs in the queue")]
        public async Task Queue()
            => await ReplyAsync(embed: _musicService.ListQueue(Context.Guild));

        [Command("pause")]
        [Summary("Pauses the current song")]
        public async Task Pause()
        {
            if (!await IsInSameVoiceChannelAsBot(Context.User as SocketGuildUser)) return;

            (var embed, _) = await _musicService.PauseOrResumeAsync(Context.Guild);

            await ReplyAsync(embed: embed);
        }

        [Command("resume")]
        [Summary("Resumes the current song")]
        public async Task Resume()
        {
            if (!await IsInSameVoiceChannelAsBot(Context.User as SocketGuildUser)) return;

            await ReplyAsync(embed: await _musicService.ResumeAsync(Context.Guild));
        }

        [Command("seek")]
        [Summary("Seeks to the specified position in current song")]
        public async Task Seek(string time)
        {
            if (!await IsInSameVoiceChannelAsBot(Context.User as SocketGuildUser)) return;

            time = "00:00:" + time;

            if (TimeSpan.TryParse(time, out var timeSpan))
            {
                await ReplyAsync(embed: await _musicService.Seek(Context.Guild, seekPosition: timeSpan));
            }
            else
            {
                var embed = new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithDescription("Invalid time format")
                    .Build();

                await ReplyAsync(embed: embed);
            }
        }

        [Command("shuffle")]
        [Summary("Shuffles the queue")]
        public async Task Shuffle()
        {
            if (!await IsInSameVoiceChannelAsBot(Context.User as SocketGuildUser)) return;

            await ReplyAsync(embed: _musicService.Shuffle(Context.Guild));
        }

        [Command("volume")]
        [Summary("Sets playback volume, min - 0, max - 1000")]
        public async Task Volume(int vol)
        {
            if (!await IsInSameVoiceChannelAsBot(Context.User as SocketGuildUser)) return;

            await ReplyAsync(embed: await _musicService.SetVolumeAsync(vol, Context.Guild));
        }

        [Command("join")]
        [Summary("Joins your voice channel")]
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
            await ReplyAsync(embed: embed);

        }

        [Command("leave")]
        [Summary("Leaves your voice channel")]
        public async Task Leave()
        {
            var user = Context.User as SocketGuildUser;
            if (!await IsInSameVoiceChannelAsBot(user)) return;

            await _musicService.LeaveAsync(user!.VoiceChannel);
            var embed = new EmbedBuilder()
                .WithColor(Color.Blue)
                .WithDescription($"Bot has now left {user.VoiceChannel.Name}")
                .Build();
            await ReplyAsync(embed: embed);

        }

        [Command("cock")]
        [Summary("Plays no cock like horse cock")]
        public async Task Cock()
        {
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

            var embed = await _musicService.PlayAsync("no cock like horse cock", Context.Guild, user!, Context.Channel);

            await ReplyAsync(embed: embed);
        }

        [Command("showui")]
        [Summary("Shows the UI in case it disappeared, surely that will never happen though")]
        public async Task ShowUi()
        {
            if (!await IsInSameVoiceChannelAsBot(Context.User as SocketGuildUser)) return;

            await _musicService.ShowUi(Context.Guild);
        }

        [Command("bass")]
        [Summary("Boosts bass. Normal - 0, max - 100")]
        public async Task Bass(int intensity)
        {
            if (!await IsInSameVoiceChannelAsBot(Context.User as SocketGuildUser)) return;

            var embed = await _musicService.Bass(Context.Guild, intensity * 0.01);

            await ReplyAsync(embed: embed);
        }

        private async Task<bool> IsInVoiceChannel(SocketGuildUser? user)
        {
            if (user?.VoiceChannel == null)
            {
                var embed = new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithDescription("You need to connect to a voice channel to use this command")
                    .Build();
                await ReplyAsync(message: $"{user?.Mention}", embed: embed);

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
                    await ReplyAsync(message: $"{user.Mention}", embed: embed);

                    return false;
                }
            }

            return true;
        }
    }
}
