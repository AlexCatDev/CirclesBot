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

        public static void SendPages(this ISocketMessageChannel msgChannel, Pages pages)
        {
            var sendMessage = msgChannel.SendMessageAsync("", false, pages.GetFirst).Result;

            if (pages.PageCount > 1)
            {
                sendMessage.CreateReactionCollector((userID, emote, wasAdded) =>
                {
                    if (emote.Name == ForwardsEmote)
                        pages.GoForwards();
                    else if (emote.Name == BackwardsEmote)
                        pages.GoBackwards();

                }, new Emoji(BackwardsEmote), new Emoji(ForwardsEmote));

                pages.MessageHandle = sendMessage;
            }
        }
    }
}
