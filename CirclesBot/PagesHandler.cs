using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System.Collections.Generic;

namespace CirclesBot
{
    public static class PagesHandler
    {
        const string ForwardsEmote = "➡";
        const string BackwardsEmote = "⬅";

        private static Dictionary<ulong, Pages> pagesDict = new Dictionary<ulong, Pages>();

        public static void SendPages(this ISocketMessageChannel msgChannel, Pages pages)
        {
            var sendMessage = msgChannel.SendMessageAsync("", false, pages.GetFirst).Result;

            pages.MessageHandle = sendMessage;

            if (pages.PageCount > 1)
            {
                sendMessage.AddReactionsAsync(new IEmote[] { new Emoji(BackwardsEmote), new Emoji(ForwardsEmote) }).GetAwaiter().GetResult();

                pagesDict.Add(pages.MessageHandle.Id, pages);
            }
        }

        public static void Handle(ulong msgID, SocketReaction reaction)
        {
            if (pagesDict.TryGetValue(msgID, out Pages page))
            {
                if (reaction.Emote.Name == ForwardsEmote)
                    page.GoForwards();
                else if (reaction.Emote.Name == BackwardsEmote)
                    page.GoBackwards();
            }
        }
    }
}
