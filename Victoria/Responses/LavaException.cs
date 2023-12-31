// ReSharper disable UnusedAutoPropertyAccessor.Local
namespace Victoria.Responses
{
    using System.Text.Json.Serialization;

    /// <summary>
    ///     If LoadStatus was LoadFailed then Exception is returned.
    /// </summary>
    public struct LavaException
    {
        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("message"), JsonInclude]
        public string Message { get; internal init; }

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("severity"), JsonInclude]
        public string Severity { get; internal init; }
    }
}