namespace Skynet.UI
{
    using Discord;
    using Skynet.Entities;

    internal static class ComponentUI
    {
        public static MessageComponent WithPauseButton { get; } = new ComponentBuilder()
                .AddRow(new ActionRowBuilder()
                    .WithButton(customId: "previous", style: ButtonStyle.Secondary, emote: Icons.Previous)
                    .WithButton(customId: "stop", style: ButtonStyle.Danger, emote: Icons.Stop)
                    .WithButton(customId: "skip", style: ButtonStyle.Danger, emote: Icons.Next))
                .AddRow(new ActionRowBuilder()
                    .WithButton(customId: "fastReverse", style: ButtonStyle.Secondary, emote: Icons.FastReverse)
                    .WithButton(customId: "pause", style: ButtonStyle.Secondary, emote: Icons.Pause)
                    .WithButton(customId: "fastForward", style: ButtonStyle.Secondary, emote: Icons.FastForward))
                .AddRow(new ActionRowBuilder()
                    .WithButton(customId: "listQueue", style: ButtonStyle.Secondary, emote: Icons.List)
                    .WithButton(customId: "shuffle", style: ButtonStyle.Secondary, emote: Icons.Shuffle)
                    .WithButton(customId: "add", style: ButtonStyle.Success, emote: Icons.Add)).Build();

        public static MessageComponent WithPlayButton { get; } = new ComponentBuilder()
                .AddRow(new ActionRowBuilder()
                    .WithButton(customId: "previous", style: ButtonStyle.Secondary, emote: Icons.Previous)
                    .WithButton(customId: "stop", style: ButtonStyle.Danger, emote: Icons.Stop)
                    .WithButton(customId: "skip", style: ButtonStyle.Danger, emote: Icons.Next))
                .AddRow(new ActionRowBuilder()
                    .WithButton(customId: "fastReverse", style: ButtonStyle.Secondary, emote: Icons.FastReverse)
                    .WithButton(customId: "play", style: ButtonStyle.Secondary, emote: Icons.Play)
                    .WithButton(customId: "fastForward", style: ButtonStyle.Secondary, emote: Icons.FastForward))
                .AddRow(new ActionRowBuilder()
                    .WithButton(customId: "listQueue", style: ButtonStyle.Secondary, emote: Icons.List)
                    .WithButton(customId: "shuffle", style: ButtonStyle.Secondary, emote: Icons.Shuffle)
                    .WithButton(customId: "add", style: ButtonStyle.Success, emote: Icons.Add)).Build();

        public static MessageComponent Stopped { get; } = new ComponentBuilder()
                .AddRow(new ActionRowBuilder()
                    .WithButton(customId: "previous", style: ButtonStyle.Secondary, emote: Icons.Previous, disabled: true)
                    .WithButton(customId: "stop", style: ButtonStyle.Danger, emote: Icons.Stop, disabled: true)
                    .WithButton(customId: "skip", style: ButtonStyle.Danger, emote: Icons.Next, disabled: true))
                .AddRow(new ActionRowBuilder()
                    .WithButton(customId: "fastReverse", style: ButtonStyle.Secondary, emote: Icons.FastReverse, disabled: true)
                    .WithButton(customId: "play", style: ButtonStyle.Secondary, emote: Icons.Play, disabled: true)
                    .WithButton(customId: "fastForward", style: ButtonStyle.Secondary, emote: Icons.FastForward, disabled: true))
                .AddRow(new ActionRowBuilder()
                    .WithButton(customId: "listQueue", style: ButtonStyle.Secondary, disabled: true, emote: Icons.List)
                    .WithButton(customId: "shuffle", style: ButtonStyle.Secondary, disabled: true, emote: Icons.Shuffle)
                    .WithButton(customId: "add", style: ButtonStyle.Success, emote: Icons.Add)).Build();
    }
}
