namespace Victoria.Payloads.Player
{
    using System.Text.Json.Serialization;

    internal sealed class VolumePayload : AbstractPlayerPayload
    {
        [JsonPropertyName("volume")]
        public int Volume { get; }

        public VolumePayload(ulong guildId, int volume) : base(guildId, "volume")
        {
            Volume = volume;
        }
    }
}