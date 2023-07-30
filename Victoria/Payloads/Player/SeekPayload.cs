namespace Victoria.Payloads.Player
{
    using System;
    using System.Text.Json.Serialization;

    internal sealed class SeekPayload : AbstractPlayerPayload
    {
        [JsonPropertyName("position")]
        public long Position { get; }

        public SeekPayload(ulong guildId, TimeSpan position) : base(guildId, "seek")
        {
            Position = (long)position.TotalMilliseconds;
        }
    }
}