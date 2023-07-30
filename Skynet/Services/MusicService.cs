namespace Skynet.Services
{
    using Discord;
    using Discord.WebSocket;
    using Skynet.Entities;
    using Skynet.UI;
    using SpotifyAPI.Web;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Victoria;
    using Victoria.Node;
    using Victoria.Node.EventArgs;
    using Victoria.Player;
    using Victoria.Player.Filters;
    using Victoria.Responses.Search;

    public class MusicService
    {
        private readonly LavaNode _lavaNode;
        private readonly DiscordSocketClient _discordClient;
        private readonly Dictionary<ulong, LimitedOrderedDictionary> _requests;
        private readonly LimitedOrderedDictionary _messages;
        private readonly Config _config;
        private readonly SpotifyClient _spotifyClient;

        public MusicService(LavaNode lavaNode, DiscordSocketClient client, Config config)
        {
            _discordClient = client;
            _lavaNode = lavaNode;
            _requests = new Dictionary<ulong, LimitedOrderedDictionary>();
            _messages = new LimitedOrderedDictionary();
            _config = config;
            var spotifyConfig = SpotifyClientConfig
                .CreateDefault()
                .WithAuthenticator(new ClientCredentialsAuthenticator(config.Spotify.ClientId, config.Spotify.ClientSecret));

            _spotifyClient = new SpotifyClient(spotifyConfig);
        }

        public Task InitializeAsync()
        {
            _discordClient.Ready += ClientReadyAsync;
            _discordClient.UserVoiceStateUpdated += VoiceChannelUpdated;
            _lavaNode.OnTrackEnd += TrackFinished;
            _lavaNode.OnTrackException += TrackExceptionAsync;
            _lavaNode.OnTrackStuck += TrackStuckAsync;
            _lavaNode.OnTrackStart += TrackStartAsync;

            return Task.CompletedTask;
        }

        private async Task VoiceChannelUpdated(SocketUser user, SocketVoiceState left, SocketVoiceState joined)
        {
            if (left.VoiceChannel == null) return;

            if (left.VoiceChannel.ConnectedUsers.Any(x => x.Id == _discordClient.CurrentUser.Id)
                && left.VoiceChannel.ConnectedUsers.Count <= 1)
            {
                _ = _lavaNode.TryGetPlayer(left.VoiceChannel.Guild, out var _player);

                var embed = new EmbedBuilder()
                    .WithColor(Color.Blue)
                    .WithDescription($"Leaving {_player.VoiceChannel.Name} because it's empty")
                    .Build();
                await _player.TextChannel.SendMessageAsync(embed: embed);
                await _lavaNode.LeaveAsync(_player.VoiceChannel);
                if (_requests.TryGetValue(left.VoiceChannel.Guild.Id, out var innerDict))
                {
                    innerDict.Clear();
                }
            }
        }

        public async Task ConnectAsync(SocketVoiceChannel voiceChannel, ITextChannel textChannel)
        {
            if (_lavaNode.TryGetPlayer(voiceChannel.Guild, out _))
            {
                await LeaveAsync(voiceChannel);
            }

            await _lavaNode.JoinAsync(voiceChannel, textChannel);
        }

        public async Task LeaveAsync(SocketVoiceChannel voiceChannel)
            => await _lavaNode.LeaveAsync(voiceChannel);

        public async Task<Embed> PlayAsync(string query, IGuild guild, IUser user, IMessageChannel channel)
        {
            var queries = new List<string>();
            IUserMessage? spotifyMessage = null;

            if (query.Contains("spotify.com"))
            {
                (var embed, var titles) = await GetSpotifyTracks(query);

                if (embed != null) return embed;

                spotifyMessage = await channel.SendMessageAsync(embed: new EmbedBuilder()
                    .WithColor(Color.Blue)
                    .WithDescription("Loading songs from Spotify...")
                    .Build());
                queries.AddRange(titles);
            }
            else
            {
                queries.Add(query);
            }

            _ = _lavaNode.TryGetPlayer(guild, out var _player);

            List<Victoria.Responses.Search.SearchResponse> responses = new();

            var tasks = queries.Select(searchQuery => 
                _lavaNode.SearchAsync(
                    Uri.IsWellFormedUriString(searchQuery, UriKind.Absolute) ? SearchType.Direct : SearchType.YouTube, 
                    searchQuery)).ToArray();
            responses.AddRange(await Task.WhenAll(tasks));

            Victoria.Responses.Search.SearchResponse results = new() { Status = SearchStatus.LoadFailed };

            if (responses.Count == 1)
            {
                results = responses[0];
            }
            else if (responses.Count > 1)
            {
                var tracks = responses.Select(x => x.Tracks.First()).ToList();

                results.Status = SearchStatus.PlaylistLoaded;
                results.Tracks = tracks;
            }

            if (results.Status == SearchStatus.NoMatches || results.Status == SearchStatus.LoadFailed)
            {
                return new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithDescription("No matches found")
                    .Build();
            }

            if (results.Status == SearchStatus.PlaylistLoaded)
            {
                _player.Queue.Enqueue(results.Tracks);
                var embed = new EmbedBuilder().WithColor(Color.Blue);
                foreach (var resultTrack in results.Tracks)
                {
                    if (embed.Fields.Count < _config.MaxDiscordEmbedFields - 1)
                    {
                        embed.AddField(resultTrack.Title, $"{resultTrack.Duration.ToString().TrimStart('0').TrimStart(':')}");
                    }
                    else if (embed.Fields.Count == _config.MaxDiscordEmbedFields - 1 && results.Tracks.Count > _config.MaxDiscordEmbedFields - 1)
                    {
                        embed.WithFooter($"...and {results.Tracks.Count - _config.MaxDiscordEmbedFields - 1} more unlisted");
                    }

                    if (!_requests.ContainsKey(guild.Id))
                    {
                        _requests[guild.Id] = new LimitedOrderedDictionary();
                    }

                    _requests[guild.Id].Add(resultTrack, user.Mention);
                }

                embed = embed.WithTitle($"Queued {results.Tracks.Count} songs");

                if (_player.PlayerState == PlayerState.Playing)
                {
                    return embed.Build();
                }
                else
                {
                    _ = _player.Queue.TryDequeue(out var playlistTrack);

                    await _player.PlayAsync(playlistTrack);

                    return embed.Build();
                }
            }

            var track = results.Tracks.First();

            if (_player.PlayerState == PlayerState.Playing)
            {
                _player.Queue.Enqueue(track);
                if (!_requests.ContainsKey(guild.Id))
                {
                    _requests[guild.Id] = new LimitedOrderedDictionary();
                }

                _requests[guild.Id].Add(track, user.Mention);
                return new EmbedBuilder()
                    .WithColor(Color.Blue)
                    .WithTitle("Queued")
                    .AddField(track.Title, $"{track.Duration.ToString().TrimStart('0').TrimStart(':')}")
                    .AddField("Position", _player.Queue.Count)
                    .Build();
            }
            else
            {
                await _player.PlayAsync(track);
                if (!_requests.ContainsKey(guild.Id))
                {
                    _requests[guild.Id] = new LimitedOrderedDictionary();
                }
                _requests[guild.Id].Add(track, user.Mention);

                return new EmbedBuilder()
                    .WithColor(Color.Blue)
                    .WithDescription("Starting playback")
                    .Build();
            }
        }

        public async Task<Embed> StopAsync(IGuild guild)
        {
            _ = _lavaNode.TryGetPlayer(guild, out var _player);
            if (_player is null)
                return new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithDescription("Player isn't playing")
                    .Build();
            await _player.StopAsync();
            _player.Queue.Clear();
            if (_requests.TryGetValue(guild.Id, out var innerDict))
            {
                innerDict.Clear();
            }

            return new EmbedBuilder()
                .WithColor(Color.Blue)
                .WithDescription("Music playback stopped")
                .Build();
        }

        public async Task<Embed> SkipAsync(IGuild guild)
        {
            _ = _lavaNode.TryGetPlayer(guild, out var _player);
            if (_player is null)
            {
                return new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithTitle("Nothing in queue")
                    .Build();
            }

            if (_player.Track is null)
            {
                return new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithTitle("Nothing playing")
                    .Build();
            }

            if (_player.Queue.Count is 0)
            {
                await StopAsync(guild);

                return new EmbedBuilder()
                    .WithColor(Color.Blue)
                    .WithTitle("Skipped")
                    .AddField(_player.Track.Title, "\u200B")
                    .Build();
            }

            (var skipped, _) = await _player.SkipAsync();

            return new EmbedBuilder()
                .WithColor(Color.Blue)
                .WithTitle("Skipped")
                .WithDescription(skipped.Title)
                .Build();
        }

        public async Task<Embed> SetVolumeAsync(int vol, IGuild guild)
        {
            _ = _lavaNode.TryGetPlayer(guild, out var _player);
            if (_player is null)
                return new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithDescription("Player isn't playing")
                    .Build();

            if (vol > 1000 || vol < 1)
            {
                return new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithDescription("Please use a number between 0 and 1000")
                    .Build();
            }

            await _player.SetVolumeAsync(vol);
            return new EmbedBuilder()
                    .WithColor(Color.Blue)
                    .WithDescription($"Volume set to: {vol}")
                    .Build();
        }

        public async Task<(Embed, PlayerState)> PauseOrResumeAsync(IGuild guild)
        {
            _ = _lavaNode.TryGetPlayer(guild, out var _player);
            if (_player is null)
            {
                var embed = new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithDescription("Player isn't playing")
                    .Build();

                return (embed, PlayerState.None);
            }

            if (_player.PlayerState == PlayerState.Playing)
            {
                await _player.PauseAsync();

                var embed = new EmbedBuilder()
                    .WithColor(Color.Blue)
                    .WithDescription("Playback paused")
                    .Build();

                return (embed, _player.PlayerState);
            }
            else if (_player.PlayerState == PlayerState.Paused)
            {
                await _player.ResumeAsync();

                var embed = new EmbedBuilder()
                    .WithColor(Color.Blue)
                    .WithDescription("Playback resumed")
                    .Build();

                return (embed, _player.PlayerState);
            }

            var outerEmbed = new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithDescription("Player isn't playing")
                    .Build();

            return (outerEmbed, _player.PlayerState);
        }

        public async Task<Embed> ResumeAsync(IGuild guild)
        {
            _ = _lavaNode.TryGetPlayer(guild, out var _player);
            if (_player is null)
            {
                return new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithDescription("Player isn't playing")
                    .Build();
            }

            if (_player.PlayerState == PlayerState.Paused)
            {
                await _player.ResumeAsync();
                return new EmbedBuilder()
                    .WithColor(Color.Blue)
                    .WithDescription("Playback resumed")
                    .Build();
            }

            return new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription("Player is not paused")
                .Build();
        }

        public async Task<Embed> Seek(IGuild guild, TimeSpan? seekPosition = null, int forwardSec = 0, int reverseSec = 0)
        {
            _ = _lavaNode.TryGetPlayer(guild, out var _player);
            if (_player == null || _player.PlayerState != PlayerState.Playing)
            {
                return new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithDescription("Player isn't playing")
                    .Build();
            }

            try
            {
                if (seekPosition != null)
                {
                    await _player.SeekAsync(seekPosition.Value);
                }
                else
                {

                    var position = _player.Track.Position + TimeSpan.FromSeconds(forwardSec);
                    position -= TimeSpan.FromSeconds(reverseSec);

                    await _player.SeekAsync(position);
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                return new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithDescription("Specified position is greater than track's total duration")
                    .Build();
            }

            if (seekPosition != null)
            {
                return new EmbedBuilder()
                    .WithColor(Color.Blue)
                    .WithDescription($"Skipped to {seekPosition.Value:mm\\:ss}")
                    .Build();
            }
            else
            {
                //this ususally reports incorrect time for a few sec after a seek
                return new EmbedBuilder()
                        .WithColor(Color.Blue)
                        .WithDescription($"Skipped to {_player.Track.Position:mm\\:ss}")
                        .Build();
            }
        }

        public Embed Shuffle(IGuild guild)
        {
            _ = _lavaNode.TryGetPlayer(guild, out var _player);
            if (_player == null)
            {
                return new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithDescription("Player isn't playing")
                    .Build();
            }

            _player.Queue.Shuffle();

            return new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithDescription("Queue shuffled")
                    .Build();
        }

        public async Task<Embed> Bass(IGuild guild, double intensity)
        {
            if (intensity < 0 || intensity > 1)
            {
                return new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithDescription("Min(default) - 0. Max - 100")
                    .Build();
            }

            _ = _lavaNode.TryGetPlayer(guild, out var _player);

            await _player.ApplyFilterAsync(new LowPassFilter(), 1, new EqualizerBand[] { new EqualizerBand(0, intensity), new EqualizerBand(1, intensity), new EqualizerBand(2, intensity), new EqualizerBand(3, intensity) });

            if (intensity > 0.5)
            {
                return new EmbedBuilder()
                    .WithColor(Color.Blue)
                    .WithDescription("DROP THE BASS")
                    .Build();
            }

            return new EmbedBuilder()
                    .WithColor(Color.Blue)
                    .WithDescription($"Bass set to {100 + (intensity * 100)}%")
                    .Build();
        }

        public Embed ListQueue(IGuild guild)
        {
            _ = _lavaNode.TryGetPlayer(guild, out var _player);

            if (_player is null)
            {
                return new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithDescription("Player isn't playing")
                    .Build();
            }

            if (_player.Queue.Count == 0)
            {
                return new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithDescription("The queue is empty")
                    .Build();
            }

            var result = new EmbedBuilder().WithColor(Color.Blue);
            foreach (var track in _player.Queue)
            {
                if (result.Fields.Count == _config.MaxDiscordEmbedFields - 1)
                {
                    result = result.WithFooter($"\n...and {_player.Queue.Count - _config.MaxDiscordEmbedFields - 1} more unlisted");
                    break;
                }

                result.AddField($"{_player.Queue.ToList().IndexOf(track) + 1}. {track.Title}", track.Duration.ToString().TrimStart('0').TrimStart(':'));
            }

            return result.Build();
        }

        private async Task ClientReadyAsync()
        {
            await _lavaNode.ConnectAsync();
        }

        private async Task TrackStartAsync(TrackStartEventArg<LavaPlayer<LavaTrack>, LavaTrack> arg)
        {
            var oldMessage = (IUserMessage?)_messages[arg.Player.TextChannel.Id];
            if (oldMessage != null)
            {
                await oldMessage.DeleteAsync();
                _messages[arg.Player.TextChannel.Id] = null;
            }

            var embed = new EmbedBuilder()
                .WithColor(Color.Blue)
                .WithThumbnailUrl(await arg.Track.FetchArtworkAsync())
                .WithTitle("Now playing")
                .AddField(arg.Player.Track.Title, $"{arg.Player.Track.Duration.ToString().TrimStart('0').TrimStart(':')}" +
                    $"\n\nRequested by {_requests[arg.Player.VoiceChannel.GuildId][arg.Player.Track]}");
            if (arg.Player.Queue.Count > 0)
            {
                embed.AddField($"Next: {arg.Player.Queue.First().Title}", $"{arg.Player.Track.Duration.ToString().TrimStart('0').TrimStart(':')}");
            }

            if (_requests.TryGetValue(arg.Player.VoiceChannel.GuildId, out var innerDict))
            {
                innerDict.Remove(arg.Player.Track);
            }

            var message = await arg.Player.TextChannel.SendMessageAsync(embed: embed.Build(), components: ComponentUI.WithPauseButton);

            _messages[arg.Player.TextChannel.Id] = message;

            await _discordClient.SetGameAsync($"{arg.Player.Track.Title}");
        }

        private async Task TrackFinished(TrackEndEventArg<LavaPlayer<LavaTrack>, LavaTrack> arg)
        {
            var oldMessage = (IUserMessage?)_messages[arg.Player.TextChannel.Id];
            if (oldMessage != null)
            {
                await oldMessage.DeleteAsync();
                _messages[arg.Player.TextChannel.Id] = null;
            }

            if (arg.Reason == TrackEndReason.Stopped)
            {
                var embed = new EmbedBuilder()
                    .WithColor(Color.Blue)
                    .WithDescription("Playback stopped");
                await _discordClient.SetGameAsync($"{_config.CommandPrefix}commands");

                var message = await arg.Player.TextChannel.SendMessageAsync(embed: embed.Build(), components: ComponentUI.Stopped);
                _messages[arg.Player.TextChannel.Id] = message;

                await _discordClient.SetGameAsync($"{_config.CommandPrefix}commands");

                return;
            }

            if (arg.Reason == TrackEndReason.Replaced)
            {
                return;
            }

            if (!arg.Player.Queue.TryDequeue(out var item) || item is not LavaTrack nextTrack)
            {
                var embed = new EmbedBuilder()
                    .WithColor(Color.Blue)
                    .WithDescription("Playback stopped. There are no more tracks in the queue");
                await _discordClient.SetGameAsync($"{_config.CommandPrefix}commands");

                var message = await arg.Player.TextChannel.SendMessageAsync(embed: embed.Build(), components: ComponentUI.Stopped);
                _messages[arg.Player.TextChannel.Id] = message;

                return;
            }

            await arg.Player.PlayAsync(nextTrack);
        }

        private Task TrackStuckAsync(TrackStuckEventArg<LavaPlayer<LavaTrack>, LavaTrack> arg)
        {
            //TODO
            throw new InvalidOperationException("Track stuck");
        }

        private Task TrackExceptionAsync(TrackExceptionEventArg<LavaPlayer<LavaTrack>, LavaTrack> arg)
        {
            //TODO
            throw new InvalidOperationException("Oh no something broke");
        }

        public async Task<Embed> ShowUi(IGuild guild)
        {
            _ = _lavaNode.TryGetPlayer(guild, out var _player);

            var embed = new EmbedBuilder()
                .WithColor(Color.Blue)
                .WithThumbnailUrl(await _player.Track.FetchArtworkAsync())
                .WithTitle("Now playing")
                .AddField(_player.Track.Title, $"{_player.Track.Duration.ToString().TrimStart('0').TrimStart(':')}" +
                    $"\n\nRequested by {_requests[guild.Id][_player.Track]}");
            if (_player.Queue.Count > 0)
            {
                embed.AddField($"Next: {_player.Queue.First().Title}", $"{_player.Track.Duration.ToString().TrimStart('0').TrimStart(':')}");
            }

            var message = await _player.TextChannel.SendMessageAsync(embed: embed.Build(), components: ComponentUI.WithPauseButton);
            if (_messages.Count > 1000)
            {
                _messages.RemoveAt(0);
            }

            _messages[_player.TextChannel] = message;

            return new EmbedBuilder()
                .WithColor(Color.Blue)
                .WithDescription("Showing UI").Build();
        }

        private async Task<(Embed?, List<string>)> GetSpotifyTracks(string query)
        {
            var titleList = new List<string>();
            if (query.Contains("playlist"))
            {
                var playlistPart = query.Split("/playlist/").Last();
                var playlistId = playlistPart.Split('?').First();
                if (string.IsNullOrEmpty(playlistId))
                {
                    return (new EmbedBuilder()
                        .WithColor(Color.Red)
                        .WithDescription("Failed to get spotify playlist id")
                        .Build(), titleList);
                }

                var spotifyPlaylist = await _spotifyClient.Playlists.Get(playlistId);

                if (spotifyPlaylist?.Tracks?.Items != null)
                {
                    titleList.AddRange(spotifyPlaylist.Tracks.Items.Select(x =>
                    {
                        var track = x.Track as FullTrack;
                        return $"{string.Join(' ', track.Artists.Select(x => x.Name))} {track.Name}";
                    }).ToList());
                }
            }
            else if (query.Contains("track"))
            {
                var trackPart = query.Split("/track/").Last();
                var trackId = trackPart.Split('?').First();
                if (string.IsNullOrEmpty(trackId))
                {
                    return (new EmbedBuilder()
                        .WithColor(Color.Red)
                        .WithDescription("Failed to get spotify track id")
                        .Build(), titleList);
                }

                var spotifyTrack = await _spotifyClient.Tracks.Get(trackId);

                titleList.Add($"{string.Join(' ', spotifyTrack.Artists.Select(x => x.Name))} {spotifyTrack.Name}");
            }
            else
            {
                return (new EmbedBuilder()
                        .WithColor(Color.Red)
                        .WithDescription("Failed to parse spotify link")
                        .Build(), titleList);
            }

            return (null, titleList);
        }
    }
}
