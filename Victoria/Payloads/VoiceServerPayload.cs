namespace Victoria.Payloads
{
    using System.Text.Json.Serialization;

    internal record VoiceServerPayload
    {
        [JsonPropertyName("token")]
        public string Token { get; init; }

        [JsonPropertyName("endpoint")]
        public string Endpoint { get; init; }
    }
}