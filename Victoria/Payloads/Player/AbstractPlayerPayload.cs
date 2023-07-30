namespace Victoria.Payloads.Player
{
    using System.Text.Json.Serialization;

    internal abstract class AbstractPlayerPayload : AbstractPayload
    {
        [JsonPropertyName("guildId")]
        public string GuildId { get; }

        protected AbstractPlayerPayload(ulong guildId, string op) : base(op)
        {
            GuildId = $"{guildId}";
        }
    }
}