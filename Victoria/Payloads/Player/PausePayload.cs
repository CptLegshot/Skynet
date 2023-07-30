namespace Victoria.Payloads.Player
{
    using System.Text.Json.Serialization;

    internal sealed class PausePayload : AbstractPlayerPayload
    {
        [JsonPropertyName("pause")]
        public bool Pause { get; }

        public PausePayload(ulong guildId, bool pause) : base(guildId, "pause")
        {
            Pause = pause;
        }
    }
}