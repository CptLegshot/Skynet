// ReSharper disable UnusedAutoPropertyAccessor.Local
namespace Victoria.Responses.Search
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// 
    /// </summary>
    public struct SearchPlaylist
    {
        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("name"), JsonInclude]
        public string Name { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("selectedTrack"), JsonInclude]
        public int SelectedTrack { get; private set; }
    }
}