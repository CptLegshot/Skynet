namespace Victoria.Node.EventArgs
{
    using Victoria.Player;

    /// <summary>
    /// 
    /// </summary>
    public readonly struct TrackStartEventArg<TLavaPlayer, TLavaTrack>
        where TLavaTrack : LavaTrack
        where TLavaPlayer : LavaPlayer<TLavaTrack>
    {
        /// <summary>
        /// 
        /// </summary>
        public TLavaPlayer Player { get; internal init; }

        /// <summary>
        /// 
        /// </summary>
        public TLavaTrack Track { get; internal init; }
    }
}