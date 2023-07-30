namespace Skynet.Entities
{
    using Discord;

    public static class Icons
    {
        public static IEmote Stop
        {
            get
            {
                if (Emote.TryParse("<:stop_wh:1112841948798132274>", out var stop))
                {
                    return stop;
                }
                else
                {
                    return new Emoji("\U000023F9");
                }
            }
        }

        public static IEmote Play
        {
            get
            {
                if (Emote.TryParse("<:play_wh:1112841953307017236>", out var play))
                {
                    return play;
                }
                else
                {
                    return new Emoji("\U000025B6");
                }
            }
        }

        public static IEmote Pause
        {
            get
            {
                if (Emote.TryParse("<:pause_wh:1112841951272767498>", out var pause))
                {
                    return pause;
                }
                else
                {
                    return new Emoji("\U000023F8");
                }
            }
        }

        public static IEmote FastReverse
        {
            get
            {
                if (Emote.TryParse("<:back_wh:1112841960961605737>", out var back))
                {
                    return back;
                }
                else
                {
                    return new Emoji("\U000023EA");
                }
            }
        }

        public static IEmote FastForward
        {
            get
            {
                if (Emote.TryParse("<:forward_wh:1112841959971770509>", out var forward))
                {
                    return forward;
                }
                else
                {
                    return new Emoji("\U000023E9");
                }
            }
        }

        public static IEmote Previous
        {
            get
            {
                if (Emote.TryParse("<:previous_wh:1112841955886497952>", out var previous))
                {
                    return previous;
                }
                else
                {
                    return new Emoji("\U000023EE");
                }
            }
        }

        public static IEmote Next
        {
            get
            {
                if (Emote.TryParse("<:next_wh:1112841957543264286>", out var next))
                {
                    return next;
                }
                else
                {
                    return new Emoji("\U000023ED");
                }
            }
        }

        public static IEmote Add
        {
            get
            {
                if (Emote.TryParse("<:plus_wh:1130164616085909555>", out var plus))
                {
                    return plus;
                }
                else
                {
                    return new Emoji("\U00002795");
                }
            }
        }

        public static IEmote List
        {
            get
            {
                if (Emote.TryParse("<:list_wh:1130164570128920716>", out var list))
                {
                    return list;
                }
                else
                {
                    return new Emoji("\U0001F4C3");
                }
            }
        }

        public static IEmote Shuffle
        {
            get
            {
                if (Emote.TryParse("<:shuffle_wh:1130927499245789316>", out var shuffle))
                {
                    return shuffle;
                }
                else
                {
                    return new Emoji("\U0001F500");
                }
            }
        }
    }
}
