using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CirclesBot
{
    public static class Extensions
    {
        private class ReactionCollector
        {
            public RestUserMessage MessageHandle;
            public IEmote[] ListeningEmotes;
            public Action<ulong, IEmote, bool> OnReactionChanged;

            public bool ContainsEmote(IEmote emote)
            {
                foreach (var item in ListeningEmotes)
                {
                    if (item.Name == emote.Name)
                        return true;
                }

                return false;
            }
        }

        //Dictionary: Key: ReactedMessageID,
        private static Dictionary<ulong, ReactionCollector> activeReactionCollectors = new Dictionary<ulong, ReactionCollector>();

        static Extensions()
        {
            Program.Client.ReactionAdded += (s, e, x) =>
            {
                if (x.UserId != Program.Client.CurrentUser.Id)
                {
                    if (activeReactionCollectors.TryGetValue(x.MessageId, out ReactionCollector action))
                    {
                        if (action.ContainsEmote(x.Emote))
                        {
                            action.OnReactionChanged?.Invoke(x.UserId, x.Emote, true);
                        }
                    }
                }

                return Task.Delay(0);
            };

            Program.Client.ReactionRemoved += (s, e, x) =>
            {
                if (x.UserId != Program.Client.CurrentUser.Id)
                {
                    if (activeReactionCollectors.TryGetValue(x.MessageId, out ReactionCollector action))
                    {
                        if (action.ContainsEmote(x.Emote))
                        {
                            action.OnReactionChanged?.Invoke(x.UserId, x.Emote, false);
                        }
                    }
                }

                return Task.Delay(0);
            };
        }

        public static void CreateReactionCollector(this RestUserMessage userMessage, Action<ulong, IEmote, bool> onReactionChanged, params IEmote[] emotes)
        {
            ReactionCollector rCollector = new ReactionCollector()
            {
                ListeningEmotes = emotes,
                MessageHandle = userMessage,
                OnReactionChanged = onReactionChanged
            };
            userMessage.AddReactionsAsync(emotes);
            activeReactionCollectors.Add(userMessage.Id, rCollector);
        }


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
