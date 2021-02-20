using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CirclesBot
{
    public static class Extensions
    {
        //Credits: Stackoverflow person -> Martin Liversage
        public static IEnumerable<ReadOnlyMemory<char>> SplitInParts(this String s, Int32 partLength)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));
            if (partLength <= 0)
                throw new ArgumentException("Part length has to be positive.", nameof(partLength));

            for (var i = 0; i < s.Length; i += partLength)
                yield return s.AsMemory().Slice(i, Math.Min(partLength, s.Length - i));
        }

        public static int Clamp(this int value, int min, int max)
        {
            if (value > max)
                return max;
            if (value < min)
                return min;

            return value;
        }

        /// <summary>
        /// I fucking love linq and it's unreadable tricks, this is very bad for performance but who cares
        /// </summary>
        /// <param name="client"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static SocketGuild GetGuild(this DiscordSocketClient client, SocketMessage msg) => client.Guilds.Where((x) => x.Channels.Any(y => y.Id == msg.Channel.Id)).First();
    }
}
