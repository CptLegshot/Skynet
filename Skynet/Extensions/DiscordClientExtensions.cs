namespace Skynet.Extensions
{
    using Discord;
    using Discord.WebSocket;
    using System;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    internal static class DiscordClientExtensions
    {
        public async static Task<IMessage?> GetMessageFromLink(this DiscordSocketClient client, string messageLink)
        {
            // Extract information from the message link
            var match = Regex.Match(messageLink, @"https?://discord.com/channels/(?<guildId>\d+)/(?<channelId>\d+)/(?<messageId>\d+)");
            if (!match.Success)
            {
                Console.WriteLine("Invalid message link format.");
                return null;
            }

            // Retrieve the message using the extracted information
            var guildId = ulong.Parse(match.Groups["guildId"].Value);
            var channelId = ulong.Parse(match.Groups["channelId"].Value);
            var messageId = ulong.Parse(match.Groups["messageId"].Value);

            var guild = client.GetGuild(guildId);
            if (guild == null)
            {
                Console.WriteLine("Guild not found.");
                return null;
            }

            var channel = guild.GetTextChannel(channelId);
            if (channel == null)
            {
                Console.WriteLine("Channel not found.");
                return null;
            }

            try
            {
                var message = await channel.GetMessageAsync(messageId);
                return message;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to retrieve message: {ex.Message}");
                return null;
            }
        }
    }
}
