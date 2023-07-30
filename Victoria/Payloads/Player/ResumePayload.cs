namespace Victoria.Payloads.Player
{
    using System;
    using System.Text.Json.Serialization;

    internal sealed class ResumePayload : AbstractPayload
    {
        [JsonPropertyName("key")]
        public string Key { get; }

        [JsonPropertyName("timeout")]
        public long Timeout { get; }

        public ResumePayload(string key, TimeSpan timeout) : base("configureResuming")
        {
            Key = key;
            Timeout = (long)timeout.TotalSeconds;
        }
    }
}