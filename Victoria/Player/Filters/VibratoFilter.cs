namespace Victoria.Player.Filters
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Similar to tremolo. While tremolo oscillates the volume, vibrato oscillates the pitch.
    /// </summary>
    public struct VibratoFilter : IFilter
    {
        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("frequency")]
        public double Frequency { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("depth")]
        public double Depth { get; set; }
    }
}