using Discord;
using Discord.Rest;
using System.Collections.Generic;

namespace CirclesBot
{
    public enum PageDirection
    {
        Forwards,
        Backwards
    }

    public class LeftArrowEmote : IEmote
    {
        public string Name => "⬅";
    }

    public class RightArrowEmote : IEmote
    {
        public string Name => "➡";
    }

    public class Pages
    {
        public RestUserMessage MessageHandle;

        private List<Embed> pages = new List<Embed>();

        public int PageCount => pages.Count;

        private int pageIndex = 0;

        public void AddEmbed(Embed embed)
        {
            pages.Add(embed);
        }

        public Embed GetFirst => pages[0];

        public Embed GetCurrentPage => pages[pageIndex];

        public void Handle(PageDirection direction)
        {
            int previousPageIndex = pageIndex;

            if(direction == PageDirection.Forwards)
                pageIndex = Extensions.Clamp(pageIndex + 1, 0, pages.Count - 1);
            if(direction == PageDirection.Backwards)
                pageIndex = Extensions.Clamp(pageIndex - 1, 0, pages.Count - 1);

            if (previousPageIndex == pageIndex)
                return;

            MessageHandle.ModifyAsync(a => a.Embed = GetCurrentPage);
        }
    }
}
