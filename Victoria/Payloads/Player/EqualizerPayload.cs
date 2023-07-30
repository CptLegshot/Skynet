namespace Victoria.Payloads.Player
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;
    using Victoria.Player.Filters;

    internal sealed class EqualizerPayload : AbstractPlayerPayload
    {
        [JsonPropertyName("bands")]
        public IEnumerable<EqualizerBand> Bands { get; }

        public EqualizerPayload(ulong guildId, params EqualizerBand[] bands) : base(guildId, "equalizer")
        {
            Bands = bands;
        }
    }
}