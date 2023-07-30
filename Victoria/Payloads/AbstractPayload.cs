namespace Victoria.Payloads
{
    using System.Text.Json.Serialization;

    internal abstract class AbstractPayload
    {
        [JsonPropertyName("op")]
        public string Op { get; }

        protected AbstractPayload(string op)
        {
            Op = op;
        }
    }
}