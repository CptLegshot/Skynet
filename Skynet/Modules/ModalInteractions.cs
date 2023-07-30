namespace Skynet.Modules
{
    using Discord.Interactions;
    using Discord.WebSocket;
    using Skynet.Services;
    using System.Threading.Tasks;

    internal class ModalInteractions : InteractionModuleBase<SocketInteractionContext<SocketModal>>
    {
        private readonly MusicService _musicService;

        public ModalInteractions(MusicService musicService)
        {
            _musicService = musicService;
        }

        //Not working. Why? idk...
        [ModalInteraction(customId: "add")]
        public async Task AddAsync(InteractionContext context)
        {
            await DeferAsync();

            var embed = await _musicService.PlayAsync("test", Context.Guild, Context.User, Context.Channel);

            await FollowupAsync(embed: embed);
        }
    }
}
