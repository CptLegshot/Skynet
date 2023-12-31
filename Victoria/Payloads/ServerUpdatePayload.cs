﻿namespace Victoria.Payloads
{
    using System.Text.Json.Serialization;

    internal sealed class ServerUpdatePayload : AbstractPayload
    {
        [JsonPropertyName("guildId")]
        public string GuildId { get; init; }

        [JsonPropertyName("sessionId")]
        public string SessionId { get; init; }

        [JsonPropertyName("event")]
        public VoiceServerPayload VoiceServerPayload { get; init; }

        public ServerUpdatePayload() : base("voiceUpdate") { }
    }
}