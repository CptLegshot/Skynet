namespace Skynet.Entities
{
    public record Config
    {
        public string CommandPrefix { get; set; } = "!";
        public string Token { get; set; } = string.Empty;
        public int MaxDiscordEmbedFields { get; set; } = 10;
        public int LaunchDelay { get; set; } = 10000;
        public SpotifyConfig Spotify { get; set; }
    }

    public record SpotifyConfig
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }
}
